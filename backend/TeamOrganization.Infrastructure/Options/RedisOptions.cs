namespace TeamOrganization.Infrastructure.Options;

public class RedisOptions
{
    public const string SectionName = "RedisOptions";
    public string ConnectionString { get; set; } = null!;
    public int AuthDatabase { get; set; }
    public int MainDatabase { get; set; }
}