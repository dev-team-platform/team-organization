using TeamOrganization.Application.Models.Users;

namespace TeamOrganization.Application.Interfaces.Services.Users;

public interface IUserCommandService
{
    Task<CreateUserResponseModel> CreateUserAsync(CreateUserRequestModel model, CancellationToken cancellationToken = default);
}