using AwesomeAssertions;
using System.Text;
using System.Text.Json;
using Moq;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Exceptions;
using TeamOrganization.Infrastructure.Services.RabbitMq.Consumers;
using Xunit;

namespace TeamOrganization.Infrastructure.Tests.Services.RabbitMq;

public class UserLoggedInConsumerTests
{
    private readonly Mock<IUserCommandService> _userCommandService = new();

    [Fact]
    public async Task HandleAsync_ShouldUpdateLastLogin_WhenEventIsValid()
    {
        var lastLogin = DateTimeOffset.Parse("2026-10-20T10:30:00+00:00");
        UpdateLastLoginRequestModel? captured = null;
        _userCommandService.Setup(service => service.UpdateLastLoginAsync(
                It.IsAny<UpdateLastLoginRequestModel>(), It.IsAny<CancellationToken>()))
            .Callback<UpdateLastLoginRequestModel, CancellationToken>((model, _) => captured = model)
            .Returns(Task.CompletedTask);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            id = Guid.NewGuid(),
            source = "team-gateway",
            type = "user.logged-in.v1",
            data = new { identitySubject = "sub-123", lastLoginAt = lastLogin }
        }));

        await new UserLoggedInConsumer(_userCommandService.Object).HandleAsync(body, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.IdentitySubject.Should().Be("sub-123");
        captured.LastLoginAt.Should().Be(lastLogin);
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task HandleAsync_ShouldThrowNonRetryableMessageException_WhenEventIsMalformedOrInvalid(string json)
    {
        Func<Task> act = () => new UserLoggedInConsumer(_userCommandService.Object).HandleAsync(
            Encoding.UTF8.GetBytes(json), CancellationToken.None);
        var exception = (await act.Should().ThrowAsync<NonRetryableMessageException>()).Which;

        exception.Message.Should().NotBeEmpty();
        _userCommandService.Verify(service => service.UpdateLastLoginAsync(
            It.IsAny<UpdateLastLoginRequestModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowNonRetryableMessageException_WhenUserIsUnknown()
    {
        _userCommandService.Setup(service => service.UpdateLastLoginAsync(
                It.IsAny<UpdateLastLoginRequestModel>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("User not found"));
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            id = Guid.NewGuid(),
            source = "team-gateway",
            type = "user.logged-in.v1",
            data = new { identitySubject = "sub-123", lastLoginAt = DateTimeOffset.Parse("2026-10-20T10:30:00+00:00") }
        }));

        Func<Task> act = () => new UserLoggedInConsumer(_userCommandService.Object)
            .HandleAsync(body, CancellationToken.None);
        var exception = (await act.Should().ThrowAsync<NonRetryableMessageException>()).Which;

        exception.InnerException.Should().BeOfType<NotFoundException>();
    }
}
