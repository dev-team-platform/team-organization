using System.Text.Json;
using StackExchange.Redis;
using TeamOrganization.Application.Interfaces.Services.Cache;

namespace TeamOrganization.Infrastructure.Services;

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
        string key,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(key);

        RedisValue cachedValue = await _database
            .StringGetAsync(key)
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
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        ArgumentNullException.ThrowIfNull(value);

        var serializedValue = JsonSerializer.Serialize(value, JsonOptions);

        var redisExpiration = expiration.HasValue
            ? new Expiration(expiration.Value)
            : Expiration.Default;

        bool succeeded = await _database
            .StringSetAsync(
                key,
                serializedValue,
                redisExpiration)
            .WaitAsync(cancellationToken);

        if (!succeeded)
        {
            throw new InvalidOperationException($"Failed to set Redis cache key '{key}'.");
        }
    }

    public async Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(key);

        await _database
            .KeyDeleteAsync(key)
            .WaitAsync(cancellationToken);
    }

    private static void ValidateKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
    }
}