using System.Text.Json;
using AwesomeAssertions;
using Moq;
using TeamOrganization.Application.Interfaces.Services.Messaging;
using TeamOrganization.Application.Models.Exports;
using TeamOrganization.Application.Models.Messaging.Publishing;
using TeamOrganization.Application.Models.Messaging.Publishing.Notifications;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Constants.EventRoutingKeys;
using TeamOrganization.Domain.Constants.Notifications;
using TeamOrganization.Domain.Utils;
using TeamOrganization.Infrastructure.Services.ExportJobs;
using Xunit;

namespace TeamOrganization.Infrastructure.Tests.Services.ExportJobs;

public class ExportNotificationServiceTests
{
    [Theory]
    [InlineData(NotificationContents.Export.Status.InProgress, NotificationContents.Export.Content.InProgress)]
    [InlineData(NotificationContents.Export.Status.Successful, NotificationContents.Export.Content.Successful)]
    [InlineData(NotificationContents.Export.Status.Failed, NotificationContents.Export.Content.Failed)]
    public async Task SendAsync_ShouldPublishNotificationEvent_WhenStatusChanges(string status, string content)
    {
        var job = NewJob();
        var fileId = Guid.NewGuid();
        var publisher = new Mock<IMessagingPublisher<OutboxEvent<SendNotificationEventModel>>>();
        OutboxEvent<SendNotificationEventModel>? published = null;
        using var cancellation = new CancellationTokenSource();
        publisher.Setup(x => x.HandleAsync(It.IsAny<OutboxEvent<SendNotificationEventModel>>(), cancellation.Token))
            .Callback<OutboxEvent<SendNotificationEventModel>, CancellationToken>((value, _) => published = value)
            .Returns(Task.CompletedTask);

        await new ExportNotificationService(publisher.Object).SendAsync(job, status, fileId, cancellation.Token);

        published.Should().NotBeNull();
        published!.Type.Should().Be(EventRoutingKeys.UserExported);
        published.RoutingKey.Should().Be(EventRoutingKeys.UserExported);
        published.ThrowOnFailure.Should().BeTrue();
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(published.Data, JsonUtils.WebSerializerOptions));
        var notification = json.RootElement.GetProperty("notification");
        notification.GetProperty("notificationId").GetGuid().Should().Be(job.NotificationId);
        notification.GetProperty("content").GetString().Should().Be(content);
        notification.GetProperty("data").GetProperty("status").GetString().Should().Be(status);
        notification.GetProperty("data").GetProperty("fileStorageId").GetGuid().Should().Be(fileId);
        notification.GetProperty("recipients")[0].GetProperty("identitySubject").GetString().Should().Be("subject");
        publisher.Verify(x => x.HandleAsync(It.IsAny<OutboxEvent<SendNotificationEventModel>>(), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task SendAsync_ShouldPropagateException_WhenPublisherFails()
    {
        var publisher = new Mock<IMessagingPublisher<OutboxEvent<SendNotificationEventModel>>>();
        publisher.Setup(x => x.HandleAsync(It.IsAny<OutboxEvent<SendNotificationEventModel>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("broker unavailable"));

        var action = () => new ExportNotificationService(publisher.Object).SendAsync(NewJob(), NotificationContents.Export.Status.InProgress);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("broker unavailable");
    }

    private static ExportJobModel NewJob() => new()
    {
        JobId = Guid.NewGuid(),
        NotificationId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        IdentitySubject = "subject",
        FileName = "UserListView_01012026.xlsx",
        Data = new GetAllUsersResponseModel()
    };
}
