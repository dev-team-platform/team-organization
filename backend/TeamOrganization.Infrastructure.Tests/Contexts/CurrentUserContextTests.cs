using AwesomeAssertions;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using TeamOrganization.Domain.Exceptions;
using TeamOrganization.Infrastructure.Contexts;
using Xunit;

namespace TeamOrganization.Infrastructure.Tests.Contexts;

public class CurrentUserContextTests
{
    [Fact]
    public void IdentitySubject_ShouldReturnSubjectClaim_WhenPrincipalContainsSubClaim()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "user-42")], "test"))
        };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(value => value.HttpContext).Returns(httpContext);

        var result = new CurrentUserContext(accessor.Object).IdentitySubject;

        result.Should().Be("user-42");
    }

    [Fact]
    public void IdentitySubject_ShouldThrowUnauthorizedException_WhenSubjectClaimIsMissing()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(value => value.HttpContext).Returns(new DefaultHttpContext());

        var exception = ((Action)(() => _ = new CurrentUserContext(accessor.Object).IdentitySubject))
            .Should().Throw<UnauthorizedException>().Which;

        exception.Message.Should().Be("User is not authenticated.");
    }
}
