namespace TeamOrganization.Api.Options;

public class AuthOptions
{
    public const string SectionName = "Auth";
    public KeycloakForAdminApiOptions KeycloakForAdminApi { get; set; } = null!;
    public InternalJwtOptions InternalJwt { get; set; } = null!;
}

public class KeycloakForAdminApiOptions
{
    public string Authority { get; set; } = null!;

    public string ClientId { get; set; } = null!;

    public string ClientSecret { get; set; } = null!;
}

public class InternalJwtOptions
{
    public string Issuer { get; set; } = null!;

    public string Audience { get; set; } = null!;

    public string PublicKeyPemPath { get; set; } = null!;
}
