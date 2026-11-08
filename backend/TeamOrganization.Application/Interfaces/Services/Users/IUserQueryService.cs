using TeamOrganization.Application.Models.Users;

namespace TeamOrganization.Application.Interfaces.Services.Users;

public interface IUserQueryService
{
    Task<GetCurrentUserResponseModel> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    Task<bool> HasRolesAsync(
        IReadOnlyList<string> roleCodes,
        CancellationToken cancellationToken = default);

    Task<bool> HasPermissionsAsync(
        IReadOnlyList<string> permissionCodes,
        bool requiredAll = false,
        CancellationToken cancellationToken = default);

    Task<GetAllUsersResponseModel> GetAllUsersAsync(
        GetAllUsersRequestModel model,
        CancellationToken cancellationToken = default);
}
