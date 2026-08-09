using TeamOrganization.Application.Models.Users;

namespace TeamOrganization.Application.Interfaces.Services.Users;

public interface IUserQueryService
{
    Task<GetCurrentUserResponseModel> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}