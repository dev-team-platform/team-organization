using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using TeamOrganization.Api.Authentication;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Infrastructure.Options;
using TeamOrganization.Infrastructure.Stores;

namespace TeamOrganization.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var keycloakSection = configuration
            .GetSection(KeycloakOptions.SectionName)
            .Get<KeycloakOptions>() ??
            throw new InvalidOperationException(
                "Keycloak configuration section is missing.");

        var authCookieSection = configuration
            .GetSection(AuthCookieOptions.SectionName)
            .Get<AuthCookieOptions>() ??
            throw new InvalidOperationException(
                "Authentication configuration section is missing.");

        services.AddSingleton<RedisTicketStore>();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = AuthenticationSchemes.ApplicationCookie;
                options.DefaultSignInScheme = AuthenticationSchemes.ApplicationCookie;
                options.DefaultChallengeScheme = AuthenticationSchemes.Keycloak;
            })
            .AddCookie(
                AuthenticationSchemes.ApplicationCookie,
                options =>
                {
                    options.Cookie.Name = authCookieSection.CookieName;
                    options.Cookie.HttpOnly = authCookieSection.HttpOnly;

                    options.Cookie.SecurePolicy =
                        Enum.Parse<CookieSecurePolicy>(authCookieSection.SecurePolicy);

                    options.Cookie.SameSite =
                        Enum.Parse<SameSiteMode>(authCookieSection.SameSite);

                    options.Cookie.Path = authCookieSection.Path;
                    options.ExpireTimeSpan = authCookieSection.ExpireTimeSpan;
                    options.SlidingExpiration = authCookieSection.SlidingExpiration;

                    options.LoginPath = "/api/v1/auth/login";
                    options.AccessDeniedPath = "/api/v1/auth/access-denied";
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
                    options.RequireHttpsMetadata = !environment.IsDevelopment();

                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "preferred_username",
                        RoleClaimType = "roles"
                    };

                    options.Events = new OpenIdConnectEvents
                    {
                        OnTicketReceived = async context =>
                        {
                            var userService = context.HttpContext
                                .RequestServices
                                .GetRequiredService<IUserCommandService>();

                            var subject = context.Principal?.FindFirst("sub")?.Value;

                            if (!string.IsNullOrWhiteSpace(subject))
                            {
                                await userService.UpdateLastLoginAsync(
                                    new UpdateLastLoginRequestModel
                                    {
                                        IdentitySubject = subject,
                                        LastLoginAt = DateTimeOffset.UtcNow
                                    },
                                    context.HttpContext.RequestAborted);
                            }
                        }
                    };
                });

        services
            .AddOptions<CookieAuthenticationOptions>(AuthenticationSchemes.ApplicationCookie)
            .Configure<RedisTicketStore>((options, ticketStore) =>
            {
                options.SessionStore = ticketStore;
            });

        return services;
    }
}