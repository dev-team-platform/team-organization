using System.Threading.Channels;
using TeamOrganization.Application.Interfaces.Services.Audit;
using TeamOrganization.Domain.Entities;

namespace TeamOrganization.Infrastructure.Services.Audit;

public sealed class AuditLogBackgroundQueue : IAuditLogBackgroundQueue
{
    private readonly Channel<IReadOnlyCollection<AuditLog>> _channel =
        Channel.CreateUnbounded<IReadOnlyCollection<AuditLog>>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

    public void Enqueue(IReadOnlyCollection<AuditLog> auditLogs)
    {
        if (auditLogs.Count > 0)
        {
            _channel.Writer.TryWrite(auditLogs);
        }
    }

    public IAsyncEnumerable<IReadOnlyCollection<AuditLog>> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
