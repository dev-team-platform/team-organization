using System.Globalization;
using AwesomeAssertions;
using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using TeamOrganization.Application.Interfaces.Services.Exports;
using TeamOrganization.Application.Interfaces.Services.FileStorages;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Application.Models.Exports;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Application.Services.Exports;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Domain.Constants.Exports;
using TeamOrganization.Domain.Enums.Users;
using TeamOrganization.Infrastructure.Options;
using TeamOrganization.Infrastructure.Services.ExportJobs;
using Xunit;

namespace TeamOrganization.Infrastructure.Tests.Services.ExportJobs;

public class GenerateUserListViewReportServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GenerateAsync_ShouldBorderEveryTableCell_WhenExportContainsOrOmitsUsers(bool includeUser)
    {
        var data = new GetAllUsersResponseModel
        {
            Items = includeUser
                ? [new GetAllUsersResponseModelItem { EmployeeCode = "E001", Username = "user", Email = "user@example.com", FirstName = "User", DisplayName = "User" }]
                : []
        };

        await using var stream = await new GenerateUserListViewReportService().GenerateAsync(data, TimeZoneInfo.Utc);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet("Users");
        var lastRow = includeUser ? 2 : 1;

        foreach (var cell in sheet.Range(1, 1, lastRow, 12).Cells())
        {
            cell.Style.Border.TopBorder.Should().Be(XLBorderStyleValues.Thin);
            cell.Style.Border.BottomBorder.Should().Be(XLBorderStyleValues.Thin);
            cell.Style.Border.LeftBorder.Should().Be(XLBorderStyleValues.Thin);
            cell.Style.Border.RightBorder.Should().Be(XLBorderStyleValues.Thin);
        }
        sheet.Cell(lastRow + 1, 1).Style.Border.TopBorder.Should().Be(XLBorderStyleValues.None);
        sheet.Cell(1, 13).Style.Border.LeftBorder.Should().Be(XLBorderStyleValues.None);
    }

    [Fact]
    public async Task GenerateAsync_ShouldStyleHeaderAndInactiveRows_WhenUsersHaveDifferentStatuses()
    {
        var data = new GetAllUsersResponseModel
        {
            Items =
            [
                new GetAllUsersResponseModelItem { EmployeeCode = "E001", Username = "active", Email = "a@example.com", FirstName = "Active", DisplayName = "Active", Status = UserStatus.Active },
                new GetAllUsersResponseModelItem { EmployeeCode = "E002", Username = "inactive", Email = "i@example.com", FirstName = "Inactive", DisplayName = "Inactive", Status = UserStatus.Inactive }
            ]
        };

        await using var stream = await new GenerateUserListViewReportService().GenerateAsync(data, TimeZoneInfo.Utc);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet("Users");

        foreach (var cell in sheet.Range(1, 1, 1, 12).Cells())
        {
            cell.Style.Font.Bold.Should().BeTrue();
            cell.Style.Font.FontColor.Should().Be(XLColor.White);
            cell.Style.Fill.BackgroundColor.Should().Be(XLColor.FromHtml(ExportColors.Header));
        }
        foreach (var cell in sheet.Range(3, 1, 3, 12).Cells())
            cell.Style.Fill.BackgroundColor.Should().Be(XLColor.FromHtml(ExportColors.UserInactiveHighlight));
        sheet.Cell(2, 8).Style.Fill.BackgroundColor.Should().NotBe(XLColor.FromHtml(ExportColors.UserInactiveHighlight));
    }

    [Fact]
    public async Task GenerateAsync_ShouldSizeColumnsToContent_WhenExportContainsLongValues()
    {
        var data = new GetAllUsersResponseModel
        {
            Items = [new GetAllUsersResponseModelItem
            {
                EmployeeCode = "E001", Username = "user", Email = "a.very.long.email.address.for.export@example.com",
                FirstName = "User", DisplayName = "User", CreatedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z", CultureInfo.InvariantCulture)
            }]
        };

        await using var stream = await new GenerateUserListViewReportService().GenerateAsync(data, TimeZoneInfo.Utc);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet("Users");

        sheet.Column(3).Width.Should().BeGreaterThan(sheet.Column(2).Width);
        sheet.Column(12).Width.Should().BeGreaterThan(sheet.Column(4).Width);
    }

    [Fact]
    public async Task GenerateAsync_ShouldWriteBirthDateInDateFormat_WhenBirthDateIsProvided()
    {
        var data = new GetAllUsersResponseModel
        {
            Items = [new GetAllUsersResponseModelItem
            {
                EmployeeCode = "E001", Username = "user", Email = "user@example.com", FirstName = "User", DisplayName = "User",
                BirthDate = new DateOnly(1995, 2, 14),
                CreatedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z", CultureInfo.InvariantCulture)
            }]
        };

        await using var stream = await new GenerateUserListViewReportService().GenerateAsync(data, TimeZoneInfo.FindSystemTimeZoneById("America/New_York"));
        using var workbook = new XLWorkbook(stream);
        var birthDate = workbook.Worksheet("Users").Cell(2, 10);

        birthDate.DataType.Should().Be(XLDataType.DateTime);
        birthDate.GetFormattedString(CultureInfo.InvariantCulture).Should().Be("14-Feb-1995");
    }

    [Theory]
    [InlineData("Asia/Ho_Chi_Minh", "08-Nov-2026 20:30:45", "09-Nov-2026 00:15:20")]
    [InlineData("America/New_York", "08-Nov-2026 08:30:45", "08-Nov-2026 12:15:20")]
    public async Task GenerateAsync_ShouldFormatDateTimesInRequestedTimeZone_WhenDatesArePresent(string zone, string login, string created)
    {
        var data = new GetAllUsersResponseModel
        {
            Items = [new GetAllUsersResponseModelItem
            {
                EmployeeCode = "E001", Username = "user", Email = "user@example.com", FirstName = "User", DisplayName = "User",
                LastLoginAt = DateTimeOffset.Parse("2026-11-08T13:30:45Z", CultureInfo.InvariantCulture),
                CreatedAt = DateTimeOffset.Parse("2026-11-08T17:15:20Z", CultureInfo.InvariantCulture)
            }]
        };

        await using var stream = await new GenerateUserListViewReportService().GenerateAsync(data, TimeZoneInfo.FindSystemTimeZoneById(zone));
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet("Users");

        sheet.Cell(2, 11).DataType.Should().Be(XLDataType.DateTime);
        sheet.Cell(2, 11).GetFormattedString(CultureInfo.InvariantCulture).Should().Be(login);
        sheet.Cell(2, 12).DataType.Should().Be(XLDataType.DateTime);
        sheet.Cell(2, 12).GetFormattedString(CultureInfo.InvariantCulture).Should().Be(created);
    }

    [Fact]
    public async Task GenerateAsync_ShouldLeaveLastLoginBlank_WhenUserHasNeverLoggedIn()
    {
        var data = new GetAllUsersResponseModel
        {
            Items = [new GetAllUsersResponseModelItem
            {
                EmployeeCode = "E001", Username = "user", Email = "user@example.com", FirstName = "User", DisplayName = "User",
                LastLoginAt = null, CreatedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z", CultureInfo.InvariantCulture)
            }]
        };

        await using var stream = await new GenerateUserListViewReportService().GenerateAsync(data, TimeZoneInfo.Utc);
        using var workbook = new XLWorkbook(stream);

        workbook.Worksheet("Users").Cell(2, 11).IsEmpty().Should().BeTrue();
        workbook.Worksheet("Users").Cell(2, 10).IsEmpty().Should().BeTrue();
        workbook.Worksheet("Users").Cell(2, 12).GetFormattedString(CultureInfo.InvariantCulture).Should().Be("01-Jan-2026 00:00:00");
    }

    [Fact]
    public async Task ProcessExportUserListViewAsync_ShouldPassJobTimeZone_WhenGeneratingReport()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        var job = new ExportJobModel
        {
            JobId = Guid.NewGuid(), NotificationId = Guid.NewGuid(), UserId = Guid.NewGuid(), IdentitySubject = "subject",
            FileName = "users.xlsx", Data = new GetAllUsersResponseModel(), TimeZoneId = zone
        };
        var data = (GetAllUsersResponseModel)job.Data;
        var generator = new Mock<IGenerateReportsService<GetAllUsersResponseModel>>();
        generator.Setup(x => x.GenerateAsync(data, zone, It.IsAny<CancellationToken>())).ReturnsAsync(new MemoryStream([1]));
        var files = new Mock<IFileStorageCommandService>();
        files.Setup(x => x.CreateFileStorageAsync(It.IsAny<Stream>(), It.IsAny<FileStorage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream _, FileStorage file, CancellationToken _) => file);
        using var provider = new ServiceCollection().AddSingleton(generator.Object).AddSingleton(files.Object).BuildServiceProvider();
        var options = Microsoft.Extensions.Options.Options.Create(new ExportOptions { MaxQueueItems = 1 });
        var worker = new ExportBackgroundService(new ExportQueueService(options), provider.GetRequiredService<IServiceScopeFactory>(), options, Mock.Of<Serilog.ILogger>());

        await worker.ProcessExportUserListViewAsync(provider, job, data);

        generator.Verify(x => x.GenerateAsync(data, zone, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExportUsersAsync_ShouldIncludeRequestTimeZone_WhenQueuingJob()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        var request = new GetAllUsersRequestModel { TimeZoneId = zone };
        var users = new Mock<IUserQueryService>();
        users.Setup(x => x.GetCurrentUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetCurrentUserResponseModel { Id = Guid.NewGuid(), IdentitySubject = "subject" });
        users.Setup(x => x.GetAllUsersAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetAllUsersResponseModel());
        var queue = new Mock<IExportQueueService>();
        ExportJobModel? queued = null;
        queue.Setup(x => x.TryEnqueue(It.IsAny<ExportJobModel>()))
            .Callback<ExportJobModel>(job => queued = job).Returns(true);
        var notifications = new Mock<IExportNotificationService>();
        notifications.Setup(x => x.SendAsync(It.IsAny<ExportJobModel>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await new ExportService(users.Object, queue.Object, notifications.Object).ExportUsersAsync(request);

        queued.Should().NotBeNull();
        queued!.TimeZoneId.Id.Should().Be(zone.Id);
    }
}
