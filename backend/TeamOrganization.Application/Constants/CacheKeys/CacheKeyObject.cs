namespace TeamOrganization.Application.CacheKeys;

public class CacheKeyObject
{
    public string Key { get; set; } = null!;
    public TimeSpan? Expiration { get; set; }

    public static TimeSpan GetJitter(TimeSpan baseExpiration, int maxJitterSeconds)
    {
        var random = new Random();
        var jitterSeconds = random.Next(0, maxJitterSeconds);
        return baseExpiration + TimeSpan.FromSeconds(jitterSeconds);
    }
}