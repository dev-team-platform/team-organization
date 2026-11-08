using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using TeamOrganization.Application;
using TeamOrganization.Application.Interfaces.Services.Cache;
using TeamOrganization.Application.Interfaces.Services.Exports;
using TeamOrganization.Application.Interfaces.Services.FileStorages;
using TeamOrganization.Application.Interfaces.Services.Messaging;
using TeamOrganization.Application.Interfaces.Services.ObjectStorages;
using TeamOrganization.Application.Models.Messaging.Publishing;
using TeamOrganization.Application.Models.Messaging.Publishing.Notifications;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Application.Services.Exports;
using TeamOrganization.Infrastructure.Options;
using TeamOrganization.Infrastructure.Services.ExportJobs;
using TeamOrganization.Infrastructure.Services.RabbitMq.Publishers;
using Xunit;

namespace TeamOrganization.Infrastructure.Tests.Services.ExportJobs;

public class ExportDependencyInjectionTests
{
    [Fact]
    public async Task AddInfrastructure_ShouldResolveExportGraph_WhenApplicationServicesAreConfigured()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test",
            ["Redis:ConnectionString"] = "localhost:6379",
            ["RabbitMq:Host"] = "localhost",
            ["RabbitMq:Port"] = "5672",
            ["RabbitMq:Username"] = "test",
            ["RabbitMq:Password"] = "test",
            ["RabbitMq:VirtualHost"] = "/",
            ["Export:MaxQueueItems"] = "3",
            ["Export:RetryCount"] = "1",
            ["Export:RetryDelayInMsSeconds:0"] = "100",
            ["Export:JitterFromMsSeconds"] = "0",
            ["Export:JitterToMsSeconds"] = "1"
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(Mock.Of<Serilog.ILogger>());
        services.AddLogging();
        services.AddHttpClient();
        services.AddAppOptions(configuration);
        services.AddApplication(configuration);
        services.AddInfrastructure(configuration);
        services.RemoveAll<ICacheService>();
        services.AddSingleton(Mock.Of<ICacheService>());
        services.RemoveAll<IObjectStorageService>();
        services.AddSingleton(Mock.Of<IObjectStorageService>());
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        await using var scope = provider.CreateAsyncScope();
        await using var secondScope = provider.CreateAsyncScope();

        var queue = scope.ServiceProvider.GetRequiredService<IExportQueueService>();
        queue.Should().BeOfType<ExportQueueService>();
        secondScope.ServiceProvider.GetRequiredService<IExportQueueService>().Should().BeSameAs(queue);
        scope.ServiceProvider.GetRequiredService<IExportService>().Should().BeOfType<ExportService>();
        scope.ServiceProvider.GetRequiredService<IExportNotificationService>().Should().BeOfType<ExportNotificationService>();
        scope.ServiceProvider.GetRequiredService<IGenerateReportsService<GetAllUsersResponseModel>>()
            .Should().BeOfType<GenerateUserListViewReportService>();
        scope.ServiceProvider.GetRequiredService<IFileStorageCommandService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IMessagingPublisher<OutboxEvent<SendNotificationEventModel>>>()
            .Should().BeOfType<OutboxEventPublisher<SendNotificationEventModel>>();
        provider.GetServices<IHostedService>().OfType<ExportBackgroundService>().Should().ContainSingle();
        provider.GetRequiredService<IOptions<ExportOptions>>().Value.MaxQueueItems.Should().Be(3);
    }
}
