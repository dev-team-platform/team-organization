using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using StackExchange.Redis;
using TeamOrganization.Application.Interfaces.Contexts;
using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Application.Interfaces.Services.Cache;
using TeamOrganization.Application.Interfaces.Services.Messaging;
using TeamOrganization.Infrastructure.Contexts;
using TeamOrganization.Infrastructure.Options;
using TeamOrganization.Infrastructure.Persistence;
using TeamOrganization.Infrastructure.Persistence.Interceptors;
using TeamOrganization.Infrastructure.Repositories;
using TeamOrganization.Infrastructure.Services.Cache;
using TeamOrganization.Infrastructure.Services.RabbitMq;
using TeamOrganization.Application.Interfaces.Services.Audit;
using TeamOrganization.Infrastructure.Services.Audit;
using TeamOrganization.Application.Models.Messaging.Publishing.Notifications;
using TeamOrganization.Application.Models.Messaging.Publishing;
using TeamOrganization.Infrastructure.Services.RabbitMq.Publishers;
using TeamOrganization.Infrastructure.Services.RabbitMq.Consumers;

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
            .ValidateOnStart();

        services
            .AddOptions<AuditLogOptions>()
            .BindConfiguration(AuditLogOptions.SectionName)
            .Validate(options => options.RetryCount >= 0, "AuditLog RetryCount must be greater than or equal to 0.")
            .Validate(options => options.RetryDelayInMsSeconds != null && options.RetryDelayInMsSeconds.Count == options.RetryCount, "AuditLog RetryDelayInMsSeconds must have the same number of elements as RetryCount.")
            .Validate(options => options.JitterFromMsSeconds >= 0, "AuditLog JitterFromMsSeconds must be greater than or equal to 0.")
            .Validate(options => options.JitterToMsSeconds >= 0, "AuditLog JitterToMsSeconds must be greater than or equal to 0.")
            .Validate(options => options.JitterFromMsSeconds <= options.JitterToMsSeconds, "AuditLog JitterFromMsSeconds must be less than or equal to JitterToMsSeconds.")
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

        // RabbitMQ
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
        services.AddSingleton<RabbitMqTopologyInitializer>();
        services.AddSingleton<RabbitMqPublisherHostedService>();
        services.AddHostedService(sp => sp.GetRequiredService<RabbitMqPublisherHostedService>());
        services.AddHostedService<RabbitMqConsumerHostedService>();

        // Messaging Publishers  
        services.AddSingleton<
            IMessagingPublisherHandler<OutboxEvent<SendNotificationEventModel>>,
            OutboxEventPublisherHandler<SendNotificationEventModel>>();

        // Messaging Consumers    
        services.AddKeyedScoped<IMessagingConsumerHandler, UserLoggedInConsumer>("UserLoggedIn");

        // Audit Log
        services.AddSingleton<IAuditLogBackgroundQueue, AuditLogBackgroundQueue>();
        services.AddHostedService<AuditLogBackgroundService>();

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
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        return services;
    }
}
