namespace TeamOrganization.Infrastructure.Options;

public class AuditLogOptions
{
    public const string SectionName = "AuditLog";
    public int RetryCount { get; init; }
    public IReadOnlyList<int> RetryDelayInMsSeconds { get; init; } = [];
    public int JitterFromMsSeconds { get; init; }
    public int JitterToMsSeconds { get; init; }
}