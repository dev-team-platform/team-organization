using AwesomeAssertions;
using Moq;
using TeamOrganization.Application.Models.Messaging.Publishing;
using TeamOrganization.Infrastructure.Options;
using TeamOrganization.Infrastructure.Services.RabbitMq;
using TeamOrganization.Infrastructure.Services.RabbitMq.Publishers;
using Xunit;

namespace TeamOrganization.Infrastructure.Tests.Services.RabbitMq.Publishers;

public class OutboxEventPublisherTests
{
    [Fact]
    public async Task HandleAsync_ShouldPropagatePublishFailure_WhenThrowOnFailureIsEnabled()
    {
        var publisher = CreatePublisher();
        var message = new OutboxEvent<string> { Type = "test", RoutingKey = "test", Data = "data", ThrowOnFailure = true };

        var action = () => publisher.HandleAsync(message);

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("*OrganizationEvents*");
    }

    [Fact]
    public async Task HandleAsync_ShouldSuppressPublishFailure_WhenThrowOnFailureIsDisabled()
    {
        var publisher = CreatePublisher();
        var message = new OutboxEvent<string> { Type = "test", RoutingKey = "test", Data = "data" };

        var action = () => publisher.HandleAsync(message);

        await action.Should().NotThrowAsync();
    }

    private static OutboxEventPublisher<string> CreatePublisher()
    {
        var logger = Mock.Of<Serilog.ILogger>();
        var hosted = new RabbitMqPublisherBackgroundService(null!, null!, Microsoft.Extensions.Options.Options.Create(new RabbitMqOptions()), logger);
        return new OutboxEventPublisher<string>(hosted, logger);
    }
}
