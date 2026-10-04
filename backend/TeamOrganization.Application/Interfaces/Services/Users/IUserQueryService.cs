using TeamOrganization.Application.Models.Users;

namespace TeamOrganization.Application.Interfaces.Services.Users;

public interface IUserQueryService
{
    Task<GetCurrentUserResponseModel> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    Task<GetAllUsersResponseModel> GetAllUsersAsync(
        GetAllUsersRequestModel model,
        CancellationToken cancellationToken = default);
}
