using TeamOrganization.Domain.Constants.CacheKeys;

namespace TeamOrganization.Application.Interfaces.Services.Cache;

public interface ICacheService
{
    Task<T?> GetAsync<T>(
        CacheKeyObject keyObj,
        CancellationToken cancellationToken = default);

    Task SetAsync<T>(
        CacheKeyObject keyObj,
        T value,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        CacheKeyObject keyObj,
        CancellationToken cancellationToken = default);
}