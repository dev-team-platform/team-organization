namespace TeamOrganization.Infrastructure.Options;

public class KeycloakOptions
{
    public const string SectionName = "Authentication:Keycloak";

    public string Authority { get; set; } = null!;

    public string ClientId { get; set; } = null!;

    public string ClientSecret { get; set; } = null!;

    public string CallbackPath { get; set; } = null!;

    public string SignedOutCallbackPath { get; set; } = null!;
}