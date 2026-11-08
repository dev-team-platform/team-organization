using AwesomeAssertions;
using Moq;
using TeamOrganization.Application.Interfaces.Services.Exports;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Application.Models.Exports;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Application.Services.Exports;
using TeamOrganization.Domain.Constants.Notifications;
using TeamOrganization.Domain.Exceptions;
using Xunit;

namespace TeamOrganization.Application.Tests.Services.Exports;

public class ExportServiceTests
{
    private readonly Mock<IUserQueryService> _users = new();
    private readonly Mock<IExportQueueService> _queue = new();
    private readonly Mock<IExportNotificationService> _notifications = new();
    private readonly GetCurrentUserResponseModel _user = new() { Id = Guid.NewGuid(), IdentitySubject = "subject" };
    private readonly GetAllUsersResponseModel _data = new();

    public ExportServiceTests()
    {
        _users.Setup(x => x.GetCurrentUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _users.Setup(x => x.GetAllUsersAsync(It.IsAny<GetAllUsersRequestModel>(), It.IsAny<CancellationToken>())).ReturnsAsync(_data);
        _queue.Setup(x => x.TryEnqueue(It.IsAny<ExportJobModel>())).Returns(true);
    }

    [Fact]
    public async Task ExportUsersAsync_ShouldQueueSnapshotAndPublishInProgress_WhenRequestIsAccepted()
    {
        ExportJobModel? job = null;
        _queue.Setup(x => x.TryEnqueue(It.IsAny<ExportJobModel>())).Callback<ExportJobModel>(value => job = value).Returns(true);
        _notifications.Setup(x => x.SendAsync(It.IsAny<ExportJobModel>(), NotificationContents.Export.Status.InProgress, null, It.IsAny<CancellationToken>()))
            .Callback<ExportJobModel, string, Guid?, CancellationToken>((value, _, _, _) => value.Ready.Task.IsCompleted.Should().BeFalse()).Returns(Task.CompletedTask);
        var query = new GetAllUsersRequestModel { CurrentPage = 3, ItemsPerPage = 20, SearchGlobalText = "Alice", TimeZoneId = TimeZoneInfo.Utc };
        using var cancellation = new CancellationTokenSource();

        var result = await Service().ExportUsersAsync(query, cancellation.Token);

        job.Should().NotBeNull();
        job!.JobId.Should().NotBeEmpty();
        job.NotificationId.Should().NotBeEmpty();
        job.UserId.Should().Be(_user.Id);
        job.IdentitySubject.Should().Be(_user.IdentitySubject);
        job.Data.Should().BeSameAs(_data);
        job.TimeZoneId.Should().BeSameAs(query.TimeZoneId);
        job.ExpiredAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(30), TimeSpan.FromSeconds(5));
        job.FileName.Should().MatchRegex("^UserListView_[0-9]{8}_[0-9]{6}\\.xlsx$");
        result.FileName.Should().Be(job.FileName);
        (await job.Ready.Task).Should().BeTrue();
        _users.Verify(x => x.GetAllUsersAsync(query, cancellation.Token), Times.Once);
        _notifications.Verify(x => x.SendAsync(job, NotificationContents.Export.Status.InProgress, null, cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task ExportUsersAsync_ShouldCancelQueuedJob_WhenInitialNotificationFails()
    {
        ExportJobModel? job = null;
        _queue.Setup(x => x.TryEnqueue(It.IsAny<ExportJobModel>())).Callback<ExportJobModel>(value => job = value).Returns(true);
        _notifications.Setup(x => x.SendAsync(It.IsAny<ExportJobModel>(), NotificationContents.Export.Status.InProgress, null, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("broker"));

        var action = () => Service().ExportUsersAsync(new GetAllUsersRequestModel { TimeZoneId = TimeZoneInfo.Utc });

        await action.Should().ThrowAsync<InvalidOperationException>();
        job.Should().NotBeNull();
        (await job!.Ready.Task).Should().BeFalse();
    }

    [Fact]
    public async Task ExportUsersAsync_ShouldNotNotify_WhenQueueIsFull()
    {
        _queue.Setup(x => x.TryEnqueue(It.IsAny<ExportJobModel>())).Returns(false);
        var action = () => Service().ExportUsersAsync(new GetAllUsersRequestModel { TimeZoneId = TimeZoneInfo.Utc });

        await action.Should().ThrowAsync<UnprocessableEntityException>();
        _notifications.Verify(x => x.SendAsync(It.IsAny<ExportJobModel>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private ExportService Service() => new(_users.Object, _queue.Object, _notifications.Object);
}
