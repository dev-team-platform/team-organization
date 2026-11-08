namespace TeamOrganization.Application.Models.IdentityProviders;

public sealed class IdentityProviderNewUserRequestModel
{
    public required string Username { get; init; }

    public required string Email { get; init; }

    public required string FirstName { get; init; }

    public string? LastName { get; init; }
}