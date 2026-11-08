using TeamOrganization.Domain.Entities;

namespace TeamOrganization.Application.Interfaces.Services.Audit;

public interface IAuditLogBackgroundQueue
{
    void Enqueue(IReadOnlyCollection<AuditLog> auditLogs);
    IAsyncEnumerable<IReadOnlyCollection<AuditLog>> ReadAllAsync(CancellationToken cancellationToken);
}