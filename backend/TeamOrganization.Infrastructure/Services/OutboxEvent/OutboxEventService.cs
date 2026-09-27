using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TeamOrganization.Application.Interfaces.Services.OutboxEvents;
using TeamOrganization.Application.Models.OutboxEvents;
using TeamOrganization.Domain.Utils;
using TeamOrganization.Infrastructure.Options;

namespace TeamOrganization.Infrastructure.Services.OutboxEvent;

public sealed class OutboxEventService : IOutboxEventService
{
    private readonly RabbitMqConnection _rabbitMqConnection;
    private readonly IOptions<RabbitMqOptions> _rabbitMqOptions;
    private readonly Serilog.ILogger _logger;

    public OutboxEventService(
        RabbitMqConnection rabbitMqConnection,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        Serilog.ILogger logger)
    {
        _rabbitMqConnection = rabbitMqConnection;
        _rabbitMqOptions = rabbitMqOptions;
        _logger = logger;
    }

    public async Task<bool> PublishAsync<TModel>(
        OutboxEvent<TModel> outboxEvent,
        CancellationToken cancellationToken = default)
    {
        var messageId = Guid.CreateVersion7();
        var envelope = new OutboxEventEnvelope<TModel>
        {
            Id = messageId,
            Source = "team-organization",
            Type = outboxEvent.Type,
            Data = outboxEvent.Data
        };

        try
        {
            var connection = await _rabbitMqConnection.GetConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(
                new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true,
                    outstandingPublisherConfirmationsRateLimiter: null,
                    consumerDispatchConcurrency: null),
                cancellationToken);

            await channel.ExchangeDeclareAsync(
                _rabbitMqOptions.Value.EventExchange,
                ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            await channel.BasicPublishAsync(
                _rabbitMqOptions.Value.EventExchange,
                outboxEvent.RoutingKey,
                mandatory: true,
                new BasicProperties
                {
                    ContentType = "application/cloudevents+json",
                    DeliveryMode = DeliveryModes.Persistent,
                    MessageId = messageId.ToString(),
                    Type = outboxEvent.Type,
                },
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope, JsonUtils.WebSerializerOptions)),
                cancellationToken);

            _logger.Information(
                "Published Organization integration event {MessageId} {MessageType} with routing key {RoutingKey}",
                messageId,
                outboxEvent.Type,
                outboxEvent.RoutingKey);

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.Error(
                exception,
                "Unable to publish Organization integration event {MessageType} with routing key {RoutingKey}",
                outboxEvent.Type,
                outboxEvent.RoutingKey);

            return false;
        }
    }
}

internal class OutboxEventEnvelope<TModel>
{
    public Guid Id { get; init; }
    public string Source { get; init; } = null!;
    public string Type { get; init; } = null!;
    public TModel Data { get; init; } = default!;
}
