using AwesomeAssertions;
using Moq;
using TeamOrganization.Application.Interfaces.Contexts;
using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Application.Interfaces.Services.Cache;
using TeamOrganization.Application.Models.Common;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Application.Services.Users;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Domain.Constants.CacheKeys;
using TeamOrganization.Domain.Enums.Users;
using TeamOrganization.Domain.Exceptions;
using Xunit;

namespace TeamOrganization.Application.Tests.Services.Users;

public class UserQueryServiceTests
{
    private const string IdentitySubject = "subject-123";

    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICurrentUserContext> _currentUserContext = new();
    private readonly Mock<ICacheService> _cacheService = new();

    private UserQueryService CreateService()
    {
        _currentUserContext.SetupGet(context => context.IdentitySubject).Returns(IdentitySubject);
        return new UserQueryService(
            _userRepository.Object, _currentUserContext.Object, _cacheService.Object);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldReturnCachedUser_WhenCacheContainsValidUser()
    {
        var cached = NewCurrentUser();
        _cacheService.Setup(cache => cache.GetAsync<GetCurrentUserResponseModel>(
                It.Is<CacheKeyObject>(key => key.Key == "team-organization:users:current-user:subject-123"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await CreateService().GetCurrentUserAsync();

        result.Should().BeSameAs(cached);
        _userRepository.Verify(repository => repository.FindCurrentUserByIdentitySubjectAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _cacheService.Verify(cache => cache.RemoveAsync(It.IsAny<CacheKeyObject>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldRemoveMalformedCacheAndLoadUser_WhenCachedIdentitySubjectIsBlank()
    {
        var malformed = new GetCurrentUserResponseModel { IdentitySubject = " " };
        var loaded = NewCurrentUser();
        _cacheService.Setup(cache => cache.GetAsync<GetCurrentUserResponseModel>(
                It.Is<CacheKeyObject>(key => key.Key == "team-organization:users:current-user:subject-123"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(malformed);
        _userRepository.Setup(repository => repository.FindCurrentUserByIdentitySubjectAsync(
                IdentitySubject, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loaded);

        var result = await CreateService().GetCurrentUserAsync();

        result.Should().BeSameAs(loaded);
        _cacheService.Verify(cache => cache.RemoveAsync(
            It.Is<CacheKeyObject>(key => key.Key == "team-organization:users:current-user:subject-123"), It.IsAny<CancellationToken>()), Times.Once);
        _cacheService.Verify(cache => cache.SetAsync(
            It.Is<CacheKeyObject>(key => key.Key == "team-organization:users:current-user:subject-123"
                && key.Expiration.HasValue && key.Expiration.Value >= TimeSpan.FromMinutes(30)), loaded,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        _cacheService.Setup(cache => cache.GetAsync<GetCurrentUserResponseModel>(
                It.IsAny<CacheKeyObject>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetCurrentUserResponseModel?)null);
        _userRepository.Setup(repository => repository.FindCurrentUserByIdentitySubjectAsync(
                IdentitySubject, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetCurrentUserResponseModel?)null);

        Func<Task> act = () => CreateService().GetCurrentUserAsync();
        var exception = (await act.Should().ThrowAsync<NotFoundException>()).Which;

        exception.Message.Should().Be("User not found.");
        _cacheService.Verify(cache => cache.SetAsync(
            It.IsAny<CacheKeyObject>(), It.IsAny<GetCurrentUserResponseModel>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldMapRepositoryResult_WhenQuerySucceeds()
    {
        var query = new GetAllUsersRequestModel { CurrentPage = 2, ItemsPerPage = 10 };
        var user = new GetAllUsersResponseModelItem
        {
            Id = Guid.NewGuid(), EmployeeCode = "E1", Username = "alice", Email = "a@example.test",
            FirstName = "Alice", DisplayName = "Alice", Status = UserStatus.Active, Gender = UserGender.Female
        };
        var filterResult = new FilterResult<GetAllUsersResponseModelItem>
        {
            Items = [user], CurrentPage = 2, ItemsPerPage = 10, TotalItems = 11, TotalPages = 2
        };
        _userRepository.Setup(repository => repository.FindAllUsersAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(filterResult);

        var result = await CreateService().GetAllUsersAsync(query);

        result.Items.Should().BeSameAs(filterResult.Items);
        result.CurrentPage.Should().Be(2);
        result.ItemsPerPage.Should().Be(10);
        result.TotalItems.Should().Be(11);
        result.TotalPages.Should().Be(2);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task HasPermissionsAsync_ShouldMatchRequestedPermissions_WhenOnlyOnePermissionIsGranted(bool requiredAll, bool expected)
    {
        var user = NewCurrentUser();
        user.PermissionCodes = ["users.read"];
        _cacheService.Setup(cache => cache.GetAsync<GetCurrentUserResponseModel>(It.IsAny<CacheKeyObject>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await CreateService().HasPermissionsAsync(["users.read", "users.write"], requiredAll);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task HasPermissionsAsync_ShouldRejectPermission_WhenCasingDiffers()
    {
        var user = NewCurrentUser();
        user.PermissionCodes = ["Users.Read"];
        _cacheService.Setup(cache => cache.GetAsync<GetCurrentUserResponseModel>(It.IsAny<CacheKeyObject>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await CreateService().HasPermissionsAsync(["users.read"]);

        result.Should().BeFalse();
    }

    private static GetCurrentUserResponseModel NewCurrentUser() => new()
    {
        Id = Guid.NewGuid(), IdentitySubject = IdentitySubject, Username = "alice", DisplayName = "Alice"
    };
}
