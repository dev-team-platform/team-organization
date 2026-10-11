namespace TeamOrganization.Application.Interfaces.Services.Messaging;

public interface IMessagingPublisher<in TMessage>
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
}
