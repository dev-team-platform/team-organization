namespace TeamOrganization.Application.Interfaces.Services.Messaging;

public interface IMessagingConsumer
{
    Task HandleAsync(ReadOnlyMemory<byte> body, CancellationToken cancellationToken);
}
