using TeamOrganization.Application.Models.Users;

namespace TeamOrganization.Application.Interfaces.Services.Users;

public interface IUserCommandService
{
    Task<CreateUserResponseModel> CreateNewUserAsync(CreateUserRequestModel model, CancellationToken cancellationToken = default);
    Task UpdateLastLoginAsync(UpdateLastLoginRequestModel model, CancellationToken cancellationToken = default);
    Task UpdateLastLogoutAsync(UpdateLastLogoutRequestModel model, CancellationToken cancellationToken = default);
}
