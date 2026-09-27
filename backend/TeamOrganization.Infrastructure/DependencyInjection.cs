using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using StackExchange.Redis;
using TeamOrganization.Application.Interfaces.Contexts;
using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Application.Interfaces.Services.Cache;
using TeamOrganization.Application.Interfaces.Services.OutboxEvents;
using TeamOrganization.Infrastructure.Contexts;
using TeamOrganization.Infrastructure.Options;
using TeamOrganization.Infrastructure.Persistence;
using TeamOrganization.Infrastructure.Persistence.Interceptors;
using TeamOrganization.Infrastructure.Repositories;
using TeamOrganization.Infrastructure.Services.Cache;
using TeamOrganization.Infrastructure.Services.OutboxEvent;

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
            .AddOptions<RabbitMqOptions>()
            .BindConfiguration(RabbitMqOptions.SectionName)
            .Validate(options => !string.IsNullOrWhiteSpace(options.Host), "RabbitMq Host is required.")
            .Validate(options => options.Port is > 0 and <= 65535, "RabbitMq Port must be between 1 and 65535.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Username), "RabbitMq Username is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Password), "RabbitMq Password is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.VirtualHost), "RabbitMq VirtualHost is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.EventExchange), "RabbitMq EventExchange is required.")
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

        services.AddSingleton<IConnectionFactory>(sp =>
        {
            var rabbitMq = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
            return new ConnectionFactory
            {
                HostName = rabbitMq.Host,
                Port = rabbitMq.Port,
                UserName = rabbitMq.Username,
                Password = rabbitMq.Password,
                VirtualHost = rabbitMq.VirtualHost,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true
            };
        });
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IOutboxEventService, OutboxEventService>();

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

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();

        return services;
    }
}
