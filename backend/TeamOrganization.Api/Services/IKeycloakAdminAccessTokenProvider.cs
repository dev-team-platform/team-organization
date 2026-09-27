namespace TeamOrganization.Api.Services;

public interface IKeycloakAdminAccessTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
