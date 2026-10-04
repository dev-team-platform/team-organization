using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TeamOrganization.Application.Interfaces.Services.Messaging;
using TeamOrganization.Domain.Exceptions;
using TeamOrganization.Infrastructure.Options;

namespace TeamOrganization.Infrastructure.Services.RabbitMq;

public sealed class RabbitMqConsumerHostedService : BackgroundService
{
    private readonly RabbitMqConnection _connection;
    private readonly RabbitMqTopologyInitializer _topology;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<RabbitMqOptions> _options;
    private readonly Serilog.ILogger _logger;

    public RabbitMqConsumerHostedService(
        RabbitMqConnection connection,
        RabbitMqTopologyInitializer topology,
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        Serilog.ILogger logger)
    {
        _connection = connection;
        _topology = topology;
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = _options.Value.Consumers.Select(
            consumer => RunConsumerAsync(consumer.Key, consumer.Value, stoppingToken));
        await Task.WhenAll(tasks);
    }

    private async Task RunConsumerAsync(
        string consumerName,
        RabbitMqConsumerOptions consumerOptions,
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _topology.InitializeAsync(stoppingToken);
                var connection = await _connection.GetConnectionAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(
                    new CreateChannelOptions(
                        publisherConfirmationsEnabled: true,
                        publisherConfirmationTrackingEnabled: true),
                    stoppingToken);

                await channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: consumerOptions.PrefetchCount,
                    global: false,
                    cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += (_, delivery) => HandleDeliveryAsync(
                    consumerName,
                    consumerOptions,
                    channel,
                    delivery,
                    stoppingToken);

                await channel.BasicConsumeAsync(
                    queue: consumerOptions.Queue,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                _logger.Information(
                    "RabbitMQ consumer {ConsumerName} started on queue {Queue}",
                    consumerName,
                    consumerOptions.Queue);

                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.Warning(
                    exception,
                    "RabbitMQ consumer {ConsumerName} disconnected; retrying in {DelaySeconds} seconds",
                    consumerName,
                    consumerOptions.ReconnectDelaySeconds);

                await Task.Delay(
                    TimeSpan.FromSeconds(consumerOptions.ReconnectDelaySeconds),
                    stoppingToken);
            }
        }
    }

    private async Task HandleDeliveryAsync(
        string consumerName,
        RabbitMqConsumerOptions consumerOptions,
        IChannel channel,
        BasicDeliverEventArgs delivery,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredKeyedService<IMessagingConsumerHandler>(consumerName);
            await handler.HandleAsync(delivery.Body, cancellationToken);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (NonRetryableMessageException exception)
        {
            _logger.Warning(
                exception,
                "Dead-lettering non-retryable RabbitMQ message. Consumer={ConsumerName}, Queue={Queue}, DeliveryTag={DeliveryTag}",
                consumerName,
                consumerOptions.Queue,
                delivery.DeliveryTag);
            await DeadLetterAsync(consumerOptions, channel, delivery, cancellationToken);
        }
        catch (Exception exception)
        {
            try
            {
                await RetryOrDeadLetterAsync(
                    consumerName,
                    consumerOptions,
                    channel,
                    delivery,
                    exception,
                    cancellationToken);
            }
            catch (Exception publishException)
            {
                _logger.Error(
                    publishException,
                    "Failed to route RabbitMQ delivery {DeliveryTag}; returning it to queue {Queue}",
                    delivery.DeliveryTag,
                    consumerOptions.Queue);
                await channel.BasicNackAsync(
                    delivery.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken);
            }
        }
    }

    private async Task RetryOrDeadLetterAsync(
        string consumerName,
        RabbitMqConsumerOptions consumerOptions,
        IChannel channel,
        BasicDeliverEventArgs delivery,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var retryAttempt = GetRetryAttempt(delivery.BasicProperties.Headers);
        var delays = _options.Value.Retry.Delays;

        if (retryAttempt >= delays.Count)
        {
            _logger.Error(
                exception,
                "RabbitMQ message exceeded retry limit. Consumer={ConsumerName}, Queue={Queue}, DeliveryTag={DeliveryTag}",
                consumerName,
                consumerOptions.Queue,
                delivery.DeliveryTag);
            await DeadLetterAsync(consumerOptions, channel, delivery, cancellationToken);
            return;
        }

        var retryQueue = RabbitMqTopologyInitializer.GetRetryQueueName(
            consumerOptions.Queue,
            delays[retryAttempt].Name);
        var properties = CreateRetryProperties(delivery, retryAttempt + 1);

        await channel.BasicPublishAsync(
            exchange: _options.Value.Retry.Exchange,
            routingKey: retryQueue,
            mandatory: true,
            basicProperties: properties,
            body: delivery.Body,
            cancellationToken: cancellationToken);
        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);

        _logger.Warning(
            exception,
            "RabbitMQ message scheduled for retry {RetryAttempt}. Consumer={ConsumerName}, Queue={Queue}, RetryQueue={RetryQueue}",
            retryAttempt + 1,
            consumerName,
            consumerOptions.Queue,
            retryQueue);
    }

    private async Task DeadLetterAsync(
        RabbitMqConsumerOptions consumerOptions,
        IChannel channel,
        BasicDeliverEventArgs delivery,
        CancellationToken cancellationToken)
    {
        await channel.BasicPublishAsync(
            exchange: _options.Value.Retry.DeadLetterExchange,
            routingKey: consumerOptions.Queue,
            mandatory: true,
            basicProperties: CopyProperties(delivery),
            body: delivery.Body,
            cancellationToken: cancellationToken);
        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);
    }

    private static BasicProperties CopyProperties(BasicDeliverEventArgs delivery)
    {
        var source = delivery.BasicProperties;
        return new BasicProperties
        {
            ContentType = source.ContentType,
            ContentEncoding = source.ContentEncoding,
            DeliveryMode = source.DeliveryMode,
            MessageId = source.MessageId,
            CorrelationId = source.CorrelationId,
            Type = source.Type,
            AppId = source.AppId,
            Headers = source.Headers is null
                ? null
                : new Dictionary<string, object?>(source.Headers)
        };
    }

    private BasicProperties CreateRetryProperties(BasicDeliverEventArgs delivery, int retryAttempt)
    {
        var headers = delivery.BasicProperties.Headers is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(delivery.BasicProperties.Headers);
        headers[_options.Value.Retry.AttemptHeader] = retryAttempt;

        return new BasicProperties
        {
            ContentType = delivery.BasicProperties.ContentType,
            ContentEncoding = delivery.BasicProperties.ContentEncoding,
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = delivery.BasicProperties.MessageId,
            CorrelationId = delivery.BasicProperties.CorrelationId,
            Type = delivery.BasicProperties.Type,
            AppId = delivery.BasicProperties.AppId,
            Headers = headers
        };
    }

    private int GetRetryAttempt(IDictionary<string, object?>? headers)
    {
        if (headers?.TryGetValue(_options.Value.Retry.AttemptHeader, out var value) != true || value is null)
        {
            return 0;
        }

        return value switch
        {
            byte number => number,
            short number => number,
            int number => number,
            long number when number is >= 0 and <= int.MaxValue => (int)number,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var number) => number,
            _ => 0
        };
    }
}
