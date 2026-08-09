using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using TeamOrganization.Application.CacheKeys;
using TeamOrganization.Application.Interfaces.Services.Cache;

namespace TeamOrganization.Infrastructure.Stores;

public sealed class RedisTicketStore : ITicketStore
{
    private readonly ICacheService _cacheService;

    public RedisTicketStore(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = Guid.NewGuid().ToString("N");

        await RenewAsync(key, ticket);

        return key;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var bytes = TicketSerializer.Default.Serialize(ticket);

        var authSessionKey = AuthCacheKeys.GetAuthSessionKey(key);

        var expiration = ticket.Properties.ExpiresUtc.HasValue
            ? ticket.Properties.ExpiresUtc.Value - DateTimeOffset.UtcNow
            : authSessionKey.Expiration;

        await _cacheService.SetAsync(
            authSessionKey.Key,
            bytes,
            expiration);
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var authSessionKey = AuthCacheKeys.GetAuthSessionKey(key);
        var bytes = await _cacheService.GetAsync<byte[]>(authSessionKey.Key);

        if (bytes is null)
        {
            return null;
        }

        return TicketSerializer.Default.Deserialize(bytes);
    }

    public async Task RemoveAsync(string key)
    {
        var authSessionKey = AuthCacheKeys.GetAuthSessionKey(key);
        await _cacheService.RemoveAsync(authSessionKey.Key);
    }
}