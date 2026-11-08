using AwesomeAssertions;
using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TeamOrganization.Application.Interfaces.Services.Exports;
using TeamOrganization.Application.Interfaces.Services.FileStorages;
using TeamOrganization.Application.Models.Exports;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Domain.Enums.FileStorage;
using TeamOrganization.Infrastructure.Services.ExportJobs;
using TeamOrganization.Infrastructure.Options;
using TeamOrganization.Domain.Constants.Notifications;
using Xunit;

namespace TeamOrganization.Infrastructure.Tests.Services.ExportJobs;

public class ExportBackgroundServiceTests
{
    [Fact]
    public async Task ProcessAsync_ShouldSaveFileAndPublishSuccess_WhenGenerationSucceeds()
    {
        var generator = new Mock<IGenerateReportsService<GetAllUsersResponseModel>>();
        generator.Setup(x => x.GenerateAsync(It.IsAny<GetAllUsersResponseModel>(), It.IsAny<TimeZoneInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(new MemoryStream([1, 2, 3]));
        var files = new Mock<IFileStorageCommandService>();
        FileStorage? saved = null;
        files.Setup(x => x.CreateFileStorageAsync(It.IsAny<Stream>(), It.IsAny<FileStorage>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, FileStorage, CancellationToken>((_, file, _) => saved = file)
            .ReturnsAsync((Stream _, FileStorage file, CancellationToken _) => file);
        var notifications = new Mock<IExportNotificationService>();
        var job = NewJob();
        using var provider = Services(generator.Object, files.Object, notifications.Object);
        var service = CreateWorker(provider);

        await service.ProcessAsync(job, CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.FileType.Should().Be(FileType.UserListView); saved.FileSize.Should().Be(3); saved.CreatedById.Should().Be(job.UserId);
        notifications.Verify(x => x.SendAsync(job, NotificationContents.Export.Status.Successful, saved.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_ShouldPublishFailed_WhenReportGenerationFails()
    {
        var generator = new Mock<IGenerateReportsService<GetAllUsersResponseModel>>();
        generator.Setup(x => x.GenerateAsync(It.IsAny<GetAllUsersResponseModel>(), It.IsAny<TimeZoneInfo>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("failed"));
        var files = new Mock<IFileStorageCommandService>();
        var notifications = new Mock<IExportNotificationService>();
        var job = NewJob();
        using var provider = Services(generator.Object, files.Object, notifications.Object);
        var service = CreateWorker(provider);

        await service.ProcessAsync(job, CancellationToken.None);

        files.Verify(x => x.CreateFileStorageAsync(It.IsAny<Stream>(), It.IsAny<FileStorage>(), It.IsAny<CancellationToken>()), Times.Never);
        notifications.Verify(x => x.SendAsync(job, NotificationContents.Export.Status.Failed, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_ShouldPublishFailed_WhenSavingFileFails()
    {
        var generator = new Mock<IGenerateReportsService<GetAllUsersResponseModel>>();
        generator.Setup(x => x.GenerateAsync(It.IsAny<GetAllUsersResponseModel>(), It.IsAny<TimeZoneInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(new MemoryStream([1]));
        var files = new Mock<IFileStorageCommandService>();
        files.Setup(x => x.CreateFileStorageAsync(It.IsAny<Stream>(), It.IsAny<FileStorage>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("upload failed"));
        var notifications = new Mock<IExportNotificationService>();
        var job = NewJob();
        using var provider = Services(generator.Object, files.Object, notifications.Object);
        var service = CreateWorker(provider);

        await service.ProcessAsync(job, CancellationToken.None);

        notifications.Verify(x => x.SendAsync(job, NotificationContents.Export.Status.Failed, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_ShouldRetryNotificationWithoutCreatingAnotherFile_WhenPublishFails()
    {
        var generator = new Mock<IGenerateReportsService<GetAllUsersResponseModel>>();
        generator.Setup(x => x.GenerateAsync(It.IsAny<GetAllUsersResponseModel>(), It.IsAny<TimeZoneInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(new MemoryStream([1]));
        var files = new Mock<IFileStorageCommandService>();
        files.Setup(x => x.CreateFileStorageAsync(It.IsAny<Stream>(), It.IsAny<FileStorage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream _, FileStorage file, CancellationToken _) => file);
        var notifications = new Mock<IExportNotificationService>();
        notifications.SetupSequence(x => x.SendAsync(It.IsAny<ExportJobModel>(), NotificationContents.Export.Status.Successful, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("broker")).Returns(Task.CompletedTask);
        var job = NewJob();
        using var provider = Services(generator.Object, files.Object, notifications.Object);
        var service = CreateWorker(provider);

        await service.ProcessAsync(job, CancellationToken.None);

        files.Verify(x => x.CreateFileStorageAsync(It.IsAny<Stream>(), It.IsAny<FileStorage>(), It.IsAny<CancellationToken>()), Times.Once);
        notifications.Verify(x => x.SendAsync(job, NotificationContents.Export.Status.Successful, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GenerateAsync_ShouldProduceWorkbook_WhenUserRowsAreProvided()
    {
        var data = new GetAllUsersResponseModel { Items = [new GetAllUsersResponseModelItem { EmployeeCode = "E001", Username = "alice", Email = "alice@example.test", FirstName = "Alice", DisplayName = "Alice" }] };
        await using var stream = await new GenerateUserListViewReportService().GenerateAsync(data, TimeZoneInfo.Utc);
        using var workbook = new XLWorkbook(stream);
        workbook.Worksheet("Users").Cell(2, 1).GetString().Should().Be("E001");
        workbook.Worksheet("Users").Cell(2, 2).GetString().Should().Be("alice");
    }

    private static ServiceProvider Services(IGenerateReportsService<GetAllUsersResponseModel> generator, IFileStorageCommandService files, IExportNotificationService notifications) =>
        new ServiceCollection().AddSingleton(generator).AddSingleton(files).AddSingleton(notifications).BuildServiceProvider();

    private static ExportBackgroundService CreateWorker(ServiceProvider provider)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ExportOptions
        {
            MaxQueueItems = 1, RetryCount = 2, RetryDelayInMsSeconds = [0, 0],
            JitterFromMsSeconds = 0, JitterToMsSeconds = 1
        });
        return new ExportBackgroundService(new ExportQueueService(options), provider.GetRequiredService<IServiceScopeFactory>(), options, Mock.Of<Serilog.ILogger>());
    }

    private static ExportJobModel NewJob() => new()
    {
        JobId = Guid.NewGuid(), NotificationId = Guid.NewGuid(), UserId = Guid.NewGuid(), IdentitySubject = "subject",
        FileName = "UserListView_01012026.xlsx", Data = new GetAllUsersResponseModel()
    };
}
