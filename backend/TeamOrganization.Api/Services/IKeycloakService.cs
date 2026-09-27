namespace TeamOrganization.Api.Services;

public interface IKeycloakService
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    Task<string> CreateNewUserAsync(
        string accessToken,
        KeycloakNewUser user,
        string defaultPassword,
        CancellationToken cancellationToken = default);
}

public sealed class KeycloakNewUser
{
    public required string Username { get; init; }

    public required string Email { get; init; }

    public required string FirstName { get; init; }

    public string? LastName { get; init; }
}
