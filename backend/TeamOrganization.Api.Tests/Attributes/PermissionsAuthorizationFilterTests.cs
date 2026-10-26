using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Moq;
using TeamOrganization.Api.Attributes;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Exceptions;
using Xunit;

namespace TeamOrganization.Api.Tests.Attributes;

public class PermissionsAuthorizationFilterTests
{
    [Fact]
    public async Task OnAuthorizationAsync_ShouldAllowRequest_WhenUserHasRequiredPermission()
    {
        var queryService = new Mock<IUserQueryService>();
        queryService.Setup(service => service.GetCurrentUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetCurrentUserResponseModel { PermissionCodes = ["users.read"] });
        var context = CreateContext();

        await new PermissionsAuthorizationFilter(queryService.Object, ["users.read"]).OnAuthorizationAsync(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task OnAuthorizationAsync_ShouldThrowForbiddenException_WhenUserLacksRequiredPermission()
    {
        var queryService = new Mock<IUserQueryService>();
        queryService.Setup(service => service.GetCurrentUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetCurrentUserResponseModel { PermissionCodes = ["users.read"] });

        Func<Task> act = () => new PermissionsAuthorizationFilter(queryService.Object, ["users.write"])
            .OnAuthorizationAsync(CreateContext());
        var exception = (await act.Should().ThrowAsync<ForbiddenException>()).Which;

        exception.Message.Should().Be("You have not permission to perform this action");
    }

    [Fact]
    public async Task OnAuthorizationAsync_ShouldComparePermissionsOrdinally_WhenPermissionCasingDiffers()
    {
        var queryService = new Mock<IUserQueryService>();
        queryService.Setup(service => service.GetCurrentUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetCurrentUserResponseModel { PermissionCodes = ["Users.Read"] });

        Func<Task> act = () => new PermissionsAuthorizationFilter(queryService.Object, ["users.read"])
            .OnAuthorizationAsync(CreateContext());
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    private static AuthorizationFilterContext CreateContext()
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new(), new());
        return new AuthorizationFilterContext(actionContext, []);
    }
}
