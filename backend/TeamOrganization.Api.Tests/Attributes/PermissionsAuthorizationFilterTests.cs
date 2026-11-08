using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Moq;
using TeamOrganization.Api.Attributes;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Domain.Exceptions;
using Xunit;

namespace TeamOrganization.Api.Tests.Attributes;

public class PermissionsAuthorizationFilterTests
{
    [Fact]
    public async Task OnAuthorizationAsync_ShouldAllowRequest_WhenUserHasRequiredPermission()
    {
        var queryService = new Mock<IUserQueryService>();
        queryService.Setup(service => service.HasPermissionsAsync(
                It.Is<IReadOnlyList<string>>(codes => codes.Count == 1 && codes[0] == "users.read"), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var context = CreateContext();

        await new PermissionsAuthorizationFilter(queryService.Object, ["users.read"]).OnAuthorizationAsync(context);

        context.Result.Should().BeNull();
        queryService.Verify(service => service.HasPermissionsAsync(
            It.Is<IReadOnlyList<string>>(codes => codes.Count == 1 && codes[0] == "users.read"), false, context.HttpContext.RequestAborted), Times.Once);
    }

    [Fact]
    public async Task OnAuthorizationAsync_ShouldThrowForbiddenException_WhenUserLacksRequiredPermission()
    {
        var queryService = new Mock<IUserQueryService>();
        queryService.Setup(service => service.HasPermissionsAsync(
                It.Is<IReadOnlyList<string>>(codes => codes.Count == 1 && codes[0] == "users.write"), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Func<Task> act = () => new PermissionsAuthorizationFilter(queryService.Object, ["users.write"])
            .OnAuthorizationAsync(CreateContext());
        var exception = (await act.Should().ThrowAsync<ForbiddenException>()).Which;

        exception.Message.Should().Be("You have not permission to perform this action");
    }

    [Fact]
    public async Task OnAuthorizationAsync_ShouldRequireAllPermissions_WhenConfigured()
    {
        var queryService = new Mock<IUserQueryService>();
        queryService.Setup(service => service.HasPermissionsAsync(
                It.Is<IReadOnlyList<string>>(codes => codes.Count == 2 && codes[0] == "users.read" && codes[1] == "users.write"), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Func<Task> act = () => new PermissionsAuthorizationFilter(queryService.Object, ["users.read", "users.write"], requiredAll: true)
            .OnAuthorizationAsync(CreateContext());
        await act.Should().ThrowAsync<ForbiddenException>();
        queryService.Verify(service => service.HasPermissionsAsync(
            It.Is<IReadOnlyList<string>>(codes => codes.Count == 2 && codes[0] == "users.read" && codes[1] == "users.write"), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AuthorizationFilterContext CreateContext()
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new(), new());
        return new AuthorizationFilterContext(actionContext, []);
    }
}
