using TeamOrganization.Application.Interfaces.Contexts;
using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Application.Interfaces.Services.Cache;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Constants.CacheKeys;
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
            keyObj,
            cancellationToken);

        if (cachedUser is not null && !string.IsNullOrWhiteSpace(cachedUser.IdentitySubject))
        {
            return cachedUser;
        }

        if (cachedUser is not null)
        {
            await _cacheService.RemoveAsync(keyObj, cancellationToken);
        }

        var result = await _userRepository.FindCurrentUserByIdentitySubjectAsync(
            _currentUserContext.IdentitySubject,
            cancellationToken) ?? throw new NotFoundException($"User not found.");

        await _cacheService.SetAsync(keyObj, result, cancellationToken);

        return result;
    }

    public async Task<GetAllUsersResponseModel> GetAllUsersAsync(
        GetAllUsersRequestModel model,
        CancellationToken cancellationToken = default)
    {
        var result = await _userRepository.FindAllUsersAsync(model, cancellationToken);
        return GetAllUsersResponseModel.FromFilterResult(result);
    }

    public async Task<bool> HasRolesAsync(IReadOnlyList<string> roleCodes, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        return roleCodes.Contains(user.RoleCode);
    }

    public async Task<bool> HasPermissionsAsync(IReadOnlyList<string> permissionCodes, bool requiredAll = false, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        return requiredAll
            ? permissionCodes.All(requiredCode => user.PermissionCodes.Contains(requiredCode, StringComparer.Ordinal))
            : permissionCodes.Any(requiredCode => user.PermissionCodes.Contains(requiredCode, StringComparer.Ordinal));
    }
}
