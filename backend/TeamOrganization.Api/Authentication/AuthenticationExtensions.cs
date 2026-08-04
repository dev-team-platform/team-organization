using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using TeamOrganization.Infrastructure.Options;

namespace TeamOrganization.Api.Authentication;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddKeycloakAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var keycloakSection = configuration
            .GetSection(KeycloakOptions.SectionName)
            .Get<KeycloakOptions>() ??
            throw new InvalidOperationException("Keycloak configuration section is missing.");

        var authCookieSection = configuration
            .GetSection(AuthCookieOptions.SectionName)
            .Get<AuthCookieOptions>() ??
            throw new InvalidOperationException("AuthCookie configuration section is missing.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    AuthenticationSchemes.ApplicationCookie;

                options.DefaultSignInScheme =
                    AuthenticationSchemes.ApplicationCookie;

                options.DefaultChallengeScheme =
                    AuthenticationSchemes.Keycloak;
            })
            .AddCookie(
                AuthenticationSchemes.ApplicationCookie,
                options =>
                {
                    options.Cookie.Name = authCookieSection.CookieName;
                    options.Cookie.HttpOnly = authCookieSection.HttpOnly;
                    options.Cookie.SecurePolicy = Enum.Parse<CookieSecurePolicy>(authCookieSection.SecurePolicy);
                    options.Cookie.SameSite = Enum.Parse<SameSiteMode>(authCookieSection.SameSite);
                    options.Cookie.Path = authCookieSection.Path;

                    options.LoginPath = "/auth/login";
                    options.AccessDeniedPath = "/auth/access-denied";
                })
            .AddOpenIdConnect(
                AuthenticationSchemes.Keycloak,
                options =>
                {
                    options.Authority = keycloakSection.Authority;
                    options.ClientId = keycloakSection.ClientId;
                    options.ClientSecret = keycloakSection.ClientSecret;
                    options.CallbackPath = keycloakSection.CallbackPath;
                    options.SignedOutCallbackPath = keycloakSection.SignedOutCallbackPath;
                    options.SignInScheme = AuthenticationSchemes.ApplicationCookie;
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.UsePkce = true;
                    options.MapInboundClaims = false;
                    options.SaveTokens = true;
                    options.GetClaimsFromUserInfoEndpoint = true;

                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");

                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            NameClaimType = "preferred_username",
                            RoleClaimType = "roles"
                        };
                });

        return services;
    }
}