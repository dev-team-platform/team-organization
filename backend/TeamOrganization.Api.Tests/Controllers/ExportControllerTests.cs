using AwesomeAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TeamOrganization.Api.Controllers.V1;
using TeamOrganization.Api.Dtos.V1.Exports;
using TeamOrganization.Api.Dtos.V1.Users;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Api.Attributes;
using TeamOrganization.Domain.Constants.Uam;
using TeamOrganization.Application.Interfaces.Services.Exports;
using TeamOrganization.Application.Models.Exports;
using Xunit;

namespace TeamOrganization.Api.Tests.Controllers;

public class ExportControllerTests
{
    [Fact]
    public async Task ExportUsersAsync_ShouldReturnAcceptedWithFileName_WhenRequestIsValid()
    {
        var service = new Mock<IExportService>();
        const string fileName = "users.xlsx";
        using var cancellation = new CancellationTokenSource();
        service.Setup(x => x.ExportUsersAsync(It.IsAny<GetAllUsersRequestModel>(), cancellation.Token))
            .ReturnsAsync(new ExportResponseModel { FileName = fileName });
        var request = new GetAllUsersRequest { SearchGlobalText = "Alice", TimeZoneId = "UTC" };

        var result = await new ExportController(service.Object).ExportUsersAsync(request, cancellation.Token);

        var response = result.Should().BeOfType<AcceptedResult>().Which.Value.Should().BeOfType<ExportResponse>().Which;
        response.FileName.Should().Be(fileName);
        service.Verify(x => x.ExportUsersAsync(It.Is<GetAllUsersRequestModel>(model => model.SearchGlobalText == "Alice" && model.TimeZoneId.Id == "UTC"), cancellation.Token), Times.Once);
    }

    [Fact]
    public void ExportUsersAsync_ShouldRequireUserExportPermission_WhenMapped()
    {
        var attribute = typeof(ExportController).GetMethod(nameof(ExportController.ExportUsersAsync))!
            .GetCustomAttributes(typeof(PermissionsAttribute), true).Cast<PermissionsAttribute>().Single();
        attribute.Arguments.Should().NotBeNull();
        attribute.Arguments![0].Should().BeOfType<string[]>().Which.Should().ContainSingle().Which.Should().Be(PC.User.Export);
    }

    [Fact]
    public void ExportController_ShouldRequireAuthentication_WhenMapped() =>
        typeof(ExportController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Should().NotBeEmpty();
}
