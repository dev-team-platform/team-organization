namespace TeamOrganization.Application.CacheKeys;

public class AuthCacheKeys
{
    private const string AuthSession = "auth:session:{id}";

    public static CacheKeyObject GetAuthSessionKey(string id)
    {
        return new CacheKeyObject
        {
            Key = AuthSession.Replace("{id}", id),
            Expiration = TimeSpan.FromHours(8)
        };
    }
}