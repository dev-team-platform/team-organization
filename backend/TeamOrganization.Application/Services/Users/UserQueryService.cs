using TeamOrganization.Application.CacheKeys;
using TeamOrganization.Application.Interfaces.Contexts;
using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Application.Interfaces.Services.Cache;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Exceptions;

namespace TeamOrganization.Application.Services.Users;

public class UserQueryService : IUserQueryService
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ICacheService _cacheService;

    public UserQueryService(
        IUserRepository userRepository,
        ICurrentUserContext currentUserContext,
        ICacheService cacheService)
    {
        _userRepository = userRepository;
        _currentUserContext = currentUserContext;
        _cacheService = cacheService;
    }

    public async Task<GetCurrentUserResponseModel> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var keyObj = UserCacheKeys.GetCurrentUserKey(_currentUserContext.IdentitySubject);
        var cachedUser = await _cacheService.GetAsync<GetCurrentUserResponseModel>(
            keyObj.Key,
            cancellationToken);

        if (cachedUser is not null)
        {
            return cachedUser;
        }

        var result = await _userRepository.FindCurrentUserByIdentitySubjectAsync(
            _currentUserContext.IdentitySubject,
            cancellationToken) ?? throw new NotFoundException($"User not found.");

        await _cacheService.SetAsync(keyObj.Key, result, keyObj.Expiration, cancellationToken);

        return result;
    }
}