using AwesomeAssertions;
using Moq;
using TeamOrganization.Application.Interfaces.Contexts;
using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Application.Interfaces.Services.Cache;
using TeamOrganization.Application.Interfaces.Services.Messaging;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Application.Models.Messaging.Publishing;
using TeamOrganization.Application.Models.Messaging.Publishing.Notifications;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Application.Services.Users;
using TeamOrganization.Domain.Constants.CacheKeys;
using TeamOrganization.Domain.Constants.EventRoutingKeys;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Domain.Enums.Users;
using TeamOrganization.Domain.Exceptions;
using Xunit;

namespace TeamOrganization.Application.Tests.Services.Users;

public class UserCommandServiceTests
{
    private readonly Mock<Serilog.ILogger> _logger = new();
    private readonly Mock<ICurrentUserContext> _currentUserContext = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRoleRepository> _roleRepository = new();
    private readonly Mock<IUserRoleRepository> _userRoleRepository = new();
    private readonly Mock<IUserQueryService> _userQueryService = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IMessagingPublisher<OutboxEvent<SendNotificationEventModel>>> _messagingPublisher = new();

    private UserCommandService CreateService() => new(
        _logger.Object,
        _currentUserContext.Object,
        _userRepository.Object,
        _roleRepository.Object,
        _userRoleRepository.Object,
        _userQueryService.Object,
        _cacheService.Object,
        _unitOfWork.Object,
        _messagingPublisher.Object);

    [Fact]
    public async Task CreateNewUserAsync_ShouldCreateUserAndRoleAndPublishNotification_WhenRoleExists()
    {
        var creator = CurrentUser();
        var role = new Role { Id = Guid.NewGuid(), Code = "employee" };
        User? savedUser = null;
        UserRole? savedUserRole = null;
        _userQueryService.Setup(service => service.GetCurrentUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync(creator);
        _roleRepository.Setup(repository => repository.FindFirstByConditionAsync(
                It.IsAny<Func<IQueryable<Role>, IQueryable<Role>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        _userRepository.Setup(repository => repository.Add(It.IsAny<User>()))
            .Callback<User>(user => savedUser = user)
            .Returns<User>(user => user);
        _userRoleRepository.Setup(repository => repository.Add(It.IsAny<UserRole>()))
            .Callback<UserRole>(userRole => savedUserRole = userRole)
            .Returns<UserRole>(userRole => userRole);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(creator.Id.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _messagingPublisher.Setup(publisher => publisher.HandleAsync(
                It.IsAny<OutboxEvent<SendNotificationEventModel>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var request = NewCreateRequest();

        var result = await CreateService().CreateNewUserAsync(request);

        savedUser.Should().NotBeNull();
        savedUser!.IdentitySubject.Should().Be(request.IdentitySubject);
        savedUser.Username.Should().Be(request.Username);
        savedUser.Status.Should().Be(UserStatus.Active);
        savedUserRole.Should().NotBeNull();
        savedUserRole!.UserId.Should().Be(savedUser.Id);
        savedUserRole.RoleId.Should().Be(role.Id);
        result.Id.Should().Be(savedUser.Id);
        result.CreatedBy!.Id.Should().Be(creator.Id);
        result.UpdatedBy!.Name.Should().Be(creator.DisplayName);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(
            creator.Id.ToString(), It.IsAny<CancellationToken>()), Times.Once);
        _messagingPublisher.Verify(publisher => publisher.HandleAsync(
            It.Is<OutboxEvent<SendNotificationEventModel>>(message => message.Type == EventRoutingKeys.UserCreated),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateNewUserAsync_ShouldThrowNotFoundExceptionWithoutPersisting_WhenRoleIsMissing()
    {
        var creator = CurrentUser();
        _userQueryService.Setup(service => service.GetCurrentUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync(creator);
        _roleRepository.Setup(repository => repository.FindFirstByConditionAsync(
                It.IsAny<Func<IQueryable<Role>, IQueryable<Role>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        Func<Task> act = () => CreateService().CreateNewUserAsync(NewCreateRequest());
        var exception = (await act.Should().ThrowAsync<NotFoundException>()).Which;

        exception.Message.Should().ContainEquivalentOf("not found");
        _userRepository.Verify(repository => repository.Add(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _messagingPublisher.Verify(publisher => publisher.HandleAsync(
            It.IsAny<OutboxEvent<SendNotificationEventModel>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewUserAsync_ShouldReturnCreatedUser_WhenNotificationPublishIsCancelledAfterCommit()
    {
        var creator = CurrentUser();
        var role = new Role { Id = Guid.NewGuid(), Code = "employee" };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        _userQueryService.Setup(service => service.GetCurrentUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync(creator);
        _roleRepository.Setup(repository => repository.FindFirstByConditionAsync(
                It.IsAny<Func<IQueryable<Role>, IQueryable<Role>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        _userRepository.Setup(repository => repository.Add(It.IsAny<User>())).Returns<User>(user => user);
        _userRoleRepository.Setup(repository => repository.Add(It.IsAny<UserRole>())).Returns<UserRole>(link => link);
        _messagingPublisher.Setup(publisher => publisher.HandleAsync(
                It.IsAny<OutboxEvent<SendNotificationEventModel>>(), cancellation.Token))
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));

        var result = await CreateService().CreateNewUserAsync(NewCreateRequest(), cancellation.Token);

        result.Id.Should().NotBe(Guid.Empty);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(
            creator.Id.ToString(), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldUpdateFieldsCommitAndInvalidateCache_WhenUserExists()
    {
        var currentUser = CurrentUser();
        var user = new User
        {
            Id = Guid.NewGuid(), IdentitySubject = "target-sub", Username = "alice", Email = "alice@example.test",
            FirstName = "Old", LastName = "Name", DisplayName = "Old Name", EmployeeCode = "E1"
        };
        var request = new UpdateUserRequestModel
        {
            UserId = user.Id, FirstName = "New", LastName = "Name", DisplayName = "New Name",
            EmployeeCode = "E2", Gender = UserGender.Female
        };
        _userQueryService.Setup(service => service.GetCurrentUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync(currentUser);
        _userRepository.Setup(repository => repository.FindFirstByConditionAsync(
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await CreateService().UpdateUserAsync(request);

        user.FirstName.Should().Be("New");
        result.DisplayName.Should().Be("New Name");
        result.Gender.Should().Be(UserGender.Female);
        result.UpdatedBy!.Id.Should().Be(currentUser.Id);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(
            currentUser.Id.ToString(), It.IsAny<CancellationToken>()), Times.Once);
        _cacheService.Verify(cache => cache.RemoveAsync(
            UserCacheKeys.GetCurrentUserKey(user.IdentitySubject).Key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldThrowNotFoundExceptionWithoutCommit_WhenUserDoesNotExist()
    {
        var currentUser = CurrentUser();
        _userQueryService.Setup(service => service.GetCurrentUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync(currentUser);
        _userRepository.Setup(repository => repository.FindFirstByConditionAsync(
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        Func<Task> act = () => CreateService().UpdateUserAsync(new UpdateUserRequestModel
        {
            UserId = Guid.NewGuid(), FirstName = "Alice", DisplayName = "Alice", EmployeeCode = "E1"
        });
        await act.Should().ThrowAsync<NotFoundException>();

        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _cacheService.Verify(cache => cache.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLastLoginAsync_ShouldSaveWithoutAuditAndClearCache_WhenUserExists()
    {
        var model = new UpdateLastLoginRequestModel
        {
            IdentitySubject = "subject-login", LastLoginAt = DateTimeOffset.UtcNow
        };
        var user = new User { Id = Guid.NewGuid(), IdentitySubject = model.IdentitySubject };
        _userRepository.Setup(repository => repository.FindFirstByConditionAsync(
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await CreateService().UpdateLastLoginAsync(model);

        user.LastLoginAt.Should().Be(model.LastLoginAt);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesWithoutAuditAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _cacheService.Verify(cache => cache.RemoveAsync(
            UserCacheKeys.GetCurrentUserKey(model.IdentitySubject).Key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateLastLoginAsync_ShouldThrowNotFoundExceptionWithoutSaving_WhenUserDoesNotExist()
    {
        _userRepository.Setup(repository => repository.FindFirstByConditionAsync(
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        Func<Task> act = () => CreateService().UpdateLastLoginAsync(
            new UpdateLastLoginRequestModel { IdentitySubject = "missing", LastLoginAt = DateTimeOffset.UtcNow });
        await act.Should().ThrowAsync<NotFoundException>();

        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesWithoutAuditAsync(It.IsAny<CancellationToken>()), Times.Never);
        _cacheService.Verify(cache => cache.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static GetCurrentUserResponseModel CurrentUser() => new()
    {
        Id = Guid.NewGuid(), IdentitySubject = "creator-subject", DisplayName = "Creator"
    };

    private static CreateUserRequestModel NewCreateRequest() => new()
    {
        IdentitySubject = "new-subject", Username = "newuser", Email = "new@example.test",
        FirstName = "New", LastName = "User", DisplayName = "New User", EmployeeCode = "E123",
        RoleCode = "employee", Gender = UserGender.Female, BirthDate = new DateOnly(1995, 5, 20)
    };
}
