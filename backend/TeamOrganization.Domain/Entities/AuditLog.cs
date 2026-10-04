using TeamOrganization.Domain.Enums;

namespace TeamOrganization.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public AuditAction Action { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? RequestId { get; set; }
    public string? ClientActionId { get; set; }
    public string? ClientRequestId { get; set; }
    public string EntityName { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string ActorId { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}