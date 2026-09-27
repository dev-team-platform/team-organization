using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TeamOrganization.Api.Options;
using TeamOrganization.Api.Services;

namespace TeamOrganization.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddInternalJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<AuthOptions>()
            .BindConfiguration(AuthOptions.SectionName)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.InternalJwt.Issuer),
                "Auth:InternalJwt:Issuer is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.InternalJwt.Audience),
                "Auth:InternalJwt:Audience is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.InternalJwt.PublicKeyPemPath),
                "Auth:InternalJwt:PublicKeyPemPath is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.KeycloakForAdminApi.Authority),
                "Auth:KeycloakForAdminApi:Authority is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.KeycloakForAdminApi.ClientId),
                "Auth:KeycloakForAdminApi:ClientId is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.KeycloakForAdminApi.ClientSecret),
                "Auth:KeycloakForAdminApi:ClientSecret is required.")
            .ValidateOnStart();

        var authOptions = configuration
            .GetRequiredSection(AuthOptions.SectionName)
            .Get<AuthOptions>()!;

        var publicKeyPem = File.ReadAllText(authOptions.InternalJwt.PublicKeyPemPath);

        var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        var signingKey = new RsaSecurityKey(rsa);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authOptions.InternalJwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = authOptions.InternalJwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = "preferred_username",
                    RoleClaimType = "roles"
                };
            });

        return services;
    }

    public static IServiceCollection AddKeycloakAdminApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authOptions = configuration
            .GetRequiredSection(AuthOptions.SectionName)
            .Get<AuthOptions>()!;

        var authority = authOptions.KeycloakForAdminApi.Authority.TrimEnd('/') + "/";

        services.AddHttpClient(
            KeycloakAdminAccessTokenProvider.HttpClientName,
            client => client.BaseAddress = new Uri(authority, UriKind.Absolute));
        services.AddSingleton<IKeycloakAdminAccessTokenProvider, KeycloakAdminAccessTokenProvider>();

        return services;
    }
}
