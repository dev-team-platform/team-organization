using System.Text.Json;
using StackExchange.Redis;
using TeamOrganization.Application.Interfaces.Services.Cache;
using TeamOrganization.Domain.Constants.CacheKeys;

namespace TeamOrganization.Infrastructure.Services.Cache;

public class RedisService : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDatabase _database;

    public RedisService(IConnectionMultiplexer connectionMultiplexer)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);
        _database = connectionMultiplexer.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(
        CacheKeyObject keyObj,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(keyObj.Key);

        RedisValue cachedValue = await _database
            .StringGetAsync(keyObj.Key)
            .WaitAsync(cancellationToken);

        if (cachedValue.IsNull)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(
            cachedValue.ToString(),
            JsonOptions);
    }

    public async Task SetAsync<T>(
        CacheKeyObject keyObj,
        T value,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(keyObj.Key);
        ArgumentNullException.ThrowIfNull(value);

        var serializedValue = JsonSerializer.Serialize(value, JsonOptions);

        var redisExpiration = keyObj.Expiration.HasValue
            ? new Expiration(keyObj.Expiration.Value)
            : Expiration.Default;

        bool succeeded = await _database
            .StringSetAsync(
                keyObj.Key,
                serializedValue,
                redisExpiration)
            .WaitAsync(cancellationToken);

        if (!succeeded)
        {
            throw new InvalidOperationException($"Failed to set Redis cache key '{keyObj.Key}'.");
        }
    }

    public async Task RemoveAsync(
        CacheKeyObject keyObj,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(keyObj.Key);

        await _database
            .KeyDeleteAsync(keyObj.Key)
            .WaitAsync(cancellationToken);
    }

    private static void ValidateKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
    }
}