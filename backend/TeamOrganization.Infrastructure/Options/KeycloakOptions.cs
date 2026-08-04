namespace TeamOrganization.Infrastructure.Options;

public class KeycloakOptions
{
    public const string SectionName = "KeycloakOptions";

    public string Authority { get; set; } = null!;

    public string ClientId { get; set; } = null!;

    public string ClientSecret { get; set; } = null!;

    public string CallbackPath { get; set; } = "/signin-oidc";

    public string SignedOutCallbackPath { get; set; } = "/signout-callback-oidc";
}