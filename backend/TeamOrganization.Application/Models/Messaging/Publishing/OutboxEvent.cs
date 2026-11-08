namespace TeamOrganization.Application.Models.Messaging.Publishing;

public class OutboxEvent<TModel>
{
    public required string RoutingKey { get; init; }

    public required string Type { get; init; }

    public required TModel Data { get; init; }

    // Callers that need to retry or abort can opt in to publish failure propagation.
    public bool ThrowOnFailure { get; init; }
}
