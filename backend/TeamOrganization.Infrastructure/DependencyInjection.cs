using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Application.Interfaces.Services.Cache;
using TeamOrganization.Infrastructure.Options;
using TeamOrganization.Infrastructure.Persistence;
using TeamOrganization.Infrastructure.Repositories;
using TeamOrganization.Infrastructure.Services;

namespace TeamOrganization.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAppOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<RedisOptions>()
            .Bind(configuration.GetRequiredSection(RedisOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                $"{RedisOptions.SectionName}:ConnectionString is required.")
            .ValidateOnStart();

        services
            .AddOptions<KeycloakOptions>()
            .BindConfiguration(KeycloakOptions.SectionName)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Authority),
                "Keycloak Authority is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ClientId),
                "Keycloak ClientId is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ClientSecret),
                "Keycloak ClientSecret is required.")
            .ValidateOnStart();

        services
            .AddOptions<AuthCookieOptions>()
            .BindConfiguration(AuthCookieOptions.SectionName)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.CookieName),
                "AuthCookieOptions CookieName is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.SecurePolicy),
                "AuthCookieOptions SecurePolicy is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.SameSite),
                "AuthCookieOptions SameSite is required.")
            .ValidateOnStart();

        services
            .AddOptions<AppAntiforgeryOptions>()
            .BindConfiguration(AppAntiforgeryOptions.SectionName)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.HeaderName),
                "Antiforgery HeaderName is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.CookieName),
                "Antiforgery CookieName is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(
                    options.RequestTokenCookieName),
                "Antiforgery RequestTokenCookieName is required.")
            .ValidateOnStart();
        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>((options) =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisOptions = sp
                .GetRequiredService<IOptions<RedisOptions>>()
                .Value;

            var options = ConfigurationOptions.Parse(redisOptions.ConnectionString);

            options.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<ICacheService, RedisService>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
