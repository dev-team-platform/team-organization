using TeamOrganization.Application.Models.IdentityProviders;

namespace TeamOrganization.Application.Interfaces.Services.IdentityProviders;

public interface IIdentityProviderService
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    Task<string> CreateNewUserAsync(
        string accessToken,
        IdentityProviderNewUserRequestModel user,
        string defaultPassword,
        CancellationToken cancellationToken = default);

    Task DeleteUserAsync(
        string accessToken,
        string identitySubject,
        CancellationToken cancellationToken = default);
}