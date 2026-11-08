using TeamOrganization.Application.Models.Users;

namespace TeamOrganization.Application.Interfaces.Services.Users;

public interface IUserCommandService
{
    Task<CreateUserResponseModel> CreateNewUserAsync(CreateUserRequestModel model, CancellationToken cancellationToken = default);
    Task<UpdateUserResponseModel> UpdateUserAsync(UpdateUserRequestModel model, CancellationToken cancellationToken = default);
    Task<DisableUserResponseModel> DisableUserAsync(DisableUserRequestModel model, CancellationToken cancellationToken = default);
    Task<DisableUserConfirmationResponseModel> ConfirmDisableUserAsync(
        DisableUserConfirmationRequestModel model,
        CancellationToken cancellationToken = default);
    Task<ChangeRoleResponseModel> ChangeRoleAsync(ChangeRoleRequestModel model, CancellationToken cancellationToken = default);
    Task<ChangeRoleConfirmationResponseModel> ConfirmChangeRoleAsync(
        ChangeRoleConfirmationRequestModel model,
        CancellationToken cancellationToken = default);
    Task UpdateLastLoginAsync(UpdateLastLoginRequestModel model, CancellationToken cancellationToken = default);
}
