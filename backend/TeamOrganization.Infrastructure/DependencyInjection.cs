using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TeamOrganization.Application.Interfaces.Contexts;
using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Application.Interfaces.Services.Cache;
using TeamOrganization.Infrastructure.Contexts;
using TeamOrganization.Infrastructure.Options;
using TeamOrganization.Infrastructure.Persistence;
using TeamOrganization.Infrastructure.Repositories;
using TeamOrganization.Infrastructure.Services;
using TeamOrganization.Infrastructure.Stores;

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
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.CallbackPath),
                "Keycloak CallbackPath is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.SignedOutCallbackPath),
                "Keycloak SignedOutCallbackPath is required.")
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
        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddSingleton<AuditSaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditSaveChangesInterceptor>();

            options
                .UseNpgsql(connectionString)
                .AddInterceptors(interceptor);
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        // Cache service
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

        // Context
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();

        // Store
        services.AddSingleton<RedisTicketStore>();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();

        return services;
    }
}
