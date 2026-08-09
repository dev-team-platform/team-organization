namespace TeamOrganization.Infrastructure.Options;

public class RedisOptions
{
    public const string SectionName = "Redis";
    public string ConnectionString { get; set; } = null!;
}