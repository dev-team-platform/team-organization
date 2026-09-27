namespace TeamOrganization.Application.CacheKeys;

public static class UserCacheKeys
{
    private const string GetCurrentUser = "team-organization:users:current-user:{sub}";

    public static CacheKeyObject GetCurrentUserKey(string sub)
    {
        return new CacheKeyObject
        {
            Key = GetCurrentUser.Replace("{sub}", sub),
            Expiration = CacheKeyObject.GetJitter(TimeSpan.FromMinutes(30), 300)
        };
    }
}