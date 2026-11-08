using System.Reflection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using TeamOrganization.Api.Controllers.V1;
using Xunit;

namespace TeamOrganization.Api.Tests.Controllers;

public class RateLimitingRemovalTests
{
    [Theory]
    [InlineData(typeof(UserController))]
    [InlineData(typeof(FileStorageController))]
    public void Controllers_ShouldRequireAuthenticationWithoutLocalRateLimit_WhenMapped(Type controller)
    {
        controller.GetCustomAttributes<AuthorizeAttribute>(true).Should().NotBeEmpty();
        controller.GetCustomAttributes<EnableRateLimitingAttribute>(true).Should().BeEmpty();
        controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes<EnableRateLimitingAttribute>(true))
            .Should().BeEmpty();
    }
}
