using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TeamOrganization.Infrastructure.Options;

namespace TeamOrganization.Infrastructure.Services.RabbitMq;

public sealed class RabbitMqTopologyInitializer
{
    private readonly RabbitMqConnection _connection;
    private readonly IOptions<RabbitMqOptions> _options;
    private readonly SemaphoreSlim _initializeLock = new(1, 1);
    private volatile bool _initialized;

    public RabbitMqTopologyInitializer(
        RabbitMqConnection connection,
        IOptions<RabbitMqOptions> options)
    {
        _connection = connection;
        _options = options;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        await _initializeLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                return;
            }

            var connection = await _connection.GetConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            var declaredExchanges = new Dictionary<string, string>(StringComparer.Ordinal);

            await DeclareExchangeAsync(channel, declaredExchanges, _options.Value.Retry.Exchange, ExchangeType.Direct, cancellationToken);
            await DeclareExchangeAsync(channel, declaredExchanges, _options.Value.Retry.ReturnExchange, ExchangeType.Direct, cancellationToken);
            await DeclareExchangeAsync(channel, declaredExchanges, _options.Value.Retry.DeadLetterExchange, ExchangeType.Direct, cancellationToken);

            foreach (var publisher in _options.Value.Publishers.Values)
            {
                await DeclareExchangeAsync(
                    channel,
                    declaredExchanges,
                    publisher.Exchange,
                    publisher.ExchangeType,
                    cancellationToken);
            }

            foreach (var consumer in _options.Value.Consumers.Values)
            {
                await channel.QueueDeclareAsync(
                    queue: consumer.Queue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: cancellationToken);

                await channel.QueueBindAsync(
                    queue: consumer.Queue,
                    exchange: _options.Value.Retry.ReturnExchange,
                    routingKey: consumer.Queue,
                    arguments: null,
                    noWait: false,
                    cancellationToken: cancellationToken);

                foreach (var binding in consumer.Bindings)
                {
                    await DeclareExchangeAsync(
                        channel,
                        declaredExchanges,
                        binding.Exchange,
                        binding.ExchangeType,
                        cancellationToken);

                    foreach (var routingKey in binding.RoutingKeys)
                    {
                        await channel.QueueBindAsync(
                            queue: consumer.Queue,
                            exchange: binding.Exchange,
                            routingKey: routingKey,
                            arguments: null,
                            noWait: false,
                            cancellationToken: cancellationToken);
                    }
                }

                var deadLetterQueue = GetDeadLetterQueueName(consumer.Queue);
                await channel.QueueDeclareAsync(
                    queue: deadLetterQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: cancellationToken);
                await channel.QueueBindAsync(
                    queue: deadLetterQueue,
                    exchange: _options.Value.Retry.DeadLetterExchange,
                    routingKey: consumer.Queue,
                    arguments: null,
                    noWait: false,
                    cancellationToken: cancellationToken);

                foreach (var delay in _options.Value.Retry.Delays)
                {
                    var retryQueue = GetRetryQueueName(consumer.Queue, delay.Name);
                    await channel.QueueDeclareAsync(
                        queue: retryQueue,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: new Dictionary<string, object?>
                        {
                            ["x-message-ttl"] = delay.MessageTtlMilliseconds,
                            ["x-dead-letter-exchange"] = _options.Value.Retry.ReturnExchange,
                            ["x-dead-letter-routing-key"] = consumer.Queue
                        },
                        cancellationToken: cancellationToken);
                    await channel.QueueBindAsync(
                        queue: retryQueue,
                        exchange: _options.Value.Retry.Exchange,
                        routingKey: retryQueue,
                        arguments: null,
                        noWait: false,
                        cancellationToken: cancellationToken);
                }
            }

            _initialized = true;
        }
        finally
        {
            _initializeLock.Release();
        }
    }

    private static async Task DeclareExchangeAsync(
        IChannel channel,
        IDictionary<string, string> declaredExchanges,
        string exchange,
        string exchangeType,
        CancellationToken cancellationToken)
    {
        if (declaredExchanges.TryGetValue(exchange, out var existingType))
        {
            if (!string.Equals(existingType, exchangeType, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"RabbitMQ exchange '{exchange}' has conflicting configured types '{existingType}' and '{exchangeType}'.");
            }

            return;
        }

        await channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: exchangeType,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);
        declaredExchanges.Add(exchange, exchangeType);
    }

    public static string GetDeadLetterQueueName(string queue) => $"{queue}.dlq";

    public static string GetRetryQueueName(string queue, string delayName) => $"{queue}.retry.{delayName}";
}
