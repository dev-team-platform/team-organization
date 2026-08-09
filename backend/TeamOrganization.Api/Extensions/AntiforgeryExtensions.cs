using TeamOrganization.Infrastructure.Options;

namespace TeamOrganization.Api.Extensions;

public static class AntiforgeryExtensions
{
    public static IServiceCollection AddAppAntiforgery(this IServiceCollection services, IConfiguration configuration)
    {
        var appOptions = configuration
            .GetRequiredSection(AppAntiforgeryOptions.SectionName)
            .Get<AppAntiforgeryOptions>()
            ?? throw new InvalidOperationException("Antiforgery configuration is missing.");

        services.AddAntiforgery(options =>
        {
            options.HeaderName = appOptions.HeaderName;

            options.Cookie.Name = appOptions.CookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.Path = appOptions.Path;

            options.Cookie.SameSite =
                Enum.Parse<SameSiteMode>(
                    appOptions.SameSite,
                    ignoreCase: true);

            options.Cookie.SecurePolicy =
                Enum.Parse<CookieSecurePolicy>(
                    appOptions.SecurePolicy,
                    ignoreCase: true);
        });

        return services;
    }
}