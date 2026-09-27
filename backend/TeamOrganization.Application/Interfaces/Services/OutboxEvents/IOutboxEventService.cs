using TeamOrganization.Application.Models.OutboxEvents;

namespace TeamOrganization.Application.Interfaces.Services.OutboxEvents;

public interface IOutboxEventService
{
    Task<bool> PublishAsync<TModel>(OutboxEvent<TModel> outboxEvent, CancellationToken cancellationToken = default);
}
