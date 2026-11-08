namespace TeamOrganization.Infrastructure.Options;

public class ExportOptions
{
    public const string SectionName = "Export";
    public int MaxQueueItems { get; init; }
    public int RetryCount { get; init; }
    public IReadOnlyList<int> RetryDelayInMsSeconds { get; init; } = [];
    public int JitterFromMsSeconds { get; init; }
    public int JitterToMsSeconds { get; init; }
}