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
using TeamOrganization.Domain.Constants.Uam;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Domain.Enums.Users;
using TeamOrganization.Domain.Exceptions;
using TeamOrganization.Domain.Utils;
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
        _userQueryService.Setup(service => service.IsSuperAdminAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
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
    public async Task UpdateUserAsync_ShouldThrowForbiddenExceptionWithoutUpdating_WhenUserIsSuperAdmin()
    {
        var currentUser = CurrentUser();
        var user = new User
        {
            Id = Guid.NewGuid(), IdentitySubject = "target-sub", Username = "alice", Email = "alice@example.test",
            FirstName = "Old", LastName = "Name", DisplayName = "Old Name", EmployeeCode = "E1"
        };
        _userQueryService.Setup(service => service.GetCurrentUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync(currentUser);
        _userQueryService.Setup(service => service.IsSuperAdminAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userRepository.Setup(repository => repository.FindFirstByConditionAsync(
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        Func<Task> act = () => CreateService().UpdateUserAsync(new UpdateUserRequestModel
        {
            UserId = user.Id, FirstName = "New", LastName = "Name", DisplayName = "New Name", EmployeeCode = "E2"
        });
        var exception = (await act.Should().ThrowAsync<ForbiddenException>()).Which;

        exception.Message.Should().Be("You do not have permission to update this user");
        user.FirstName.Should().Be("Old");
        user.DisplayName.Should().Be("Old Name");
        user.EmployeeCode.Should().Be("E1");
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _cacheService.Verify(cache => cache.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
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
    public async Task DisableUserAsync_ShouldCacheHashedOtpAndPublishEmail_WhenUserIsActive()
    {
        var currentUser = CurrentUser();
        var user = new User
        {
            Id = Guid.NewGuid(), IdentitySubject = "target-subject", Username = "alice",
            Email = "alice@example.test", DisplayName = "Alice", Status = UserStatus.Active
        };
        UserActionOtpChallengeModel? savedChallenge = null;
        TimeSpan? savedExpiration = null;
        OutboxEvent<SendNotificationEventModel>? publishedEvent = null;
        _userQueryService.Setup(service => service.GetCurrentUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);
        _userQueryService.Setup(service => service.IsSuperAdminAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepository.Setup(repository => repository.FindFirstByConditionAsync(
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _cacheService.Setup(cache => cache.SetAsync(
                UserCacheKeys.GetDisableOtpKey(user.Id).Key,
                It.IsAny<UserActionOtpChallengeModel>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Callback<string, UserActionOtpChallengeModel, TimeSpan?, CancellationToken>(
                (_, challenge, expiration, _) =>
                {
                    savedChallenge = challenge;
                    savedExpiration = expiration;
                })
            .Returns(Task.CompletedTask);
        _messagingPublisher.Setup(publisher => publisher.HandleAsync(
                It.IsAny<OutboxEvent<SendNotificationEventModel>>(), It.IsAny<CancellationToken>()))
            .Callback<OutboxEvent<SendNotificationEventModel>, CancellationToken>((message, _) => publishedEvent = message)
            .Returns(Task.CompletedTask);

        var result = await CreateService().DisableUserAsync(new DisableUserRequestModel { UserId = user.Id });

        result.UserId.Should().Be(user.Id);
        result.OtpExpiresInMinutes.Should().Be(UserCacheKeys.OtpExpirationMinutes);
        savedExpiration.Should().Be(TimeSpan.FromMinutes(UserCacheKeys.OtpExpirationMinutes));
        savedChallenge.Should().NotBeNull();
        var email = publishedEvent!.Data.Email!;
        email.TemplateKey.Should().Be("DisableUser");
        email.Recipients.Single().Email.Should().Be(currentUser.Email);
        var otp = email.Data!.RootElement.GetProperty("otpCode").GetString();
        savedChallenge!.OtpHash.Should().Be(Sha256Utils.ComputeHexHash(otp!));
        savedChallenge.RequestedById.Should().Be(currentUser.Id);
        publishedEvent.Type.Should().Be(EventRoutingKeys.UserDisableOtpRequested);
    }

    [Fact]
    public async Task ConfirmDisableUserAsync_ShouldMarkUserInactiveAndConsumeOtp_WhenOtpMatches()
    {
        const string otp = "Abc1234567";
        var currentUser = CurrentUser();
        var user = new User
        {
            Id = Guid.NewGuid(), IdentitySubject = "target-subject", Status = UserStatus.Active
        };
        var otpKey = UserCacheKeys.GetDisableOtpKey(user.Id);
        _userQueryService.Setup(service => service.GetCurrentUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);
        _userQueryService.Setup(service => service.IsSuperAdminAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepository.Setup(repository => repository.FindFirstByConditionAsync(
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _cacheService.Setup(cache => cache.GetAsync<UserActionOtpChallengeModel>(
                otpKey.Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserActionOtpChallengeModel
            {
                OtpHash = Sha256Utils.ComputeHexHash(otp),
                RequestedById = currentUser.Id
            });
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(
                currentUser.Id.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await CreateService().ConfirmDisableUserAsync(new DisableUserConfirmationRequestModel
        {
            UserId = user.Id,
            Otp = otp
        });

        user.Status.Should().Be(UserStatus.Inactive);
        result.Status.Should().Be(UserStatus.Inactive);
        result.UpdatedBy!.Id.Should().Be(currentUser.Id);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(
            currentUser.Id.ToString(), It.IsAny<CancellationToken>()), Times.Once);
        _cacheService.Verify(cache => cache.RemoveAsync(otpKey.Key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmDisableUserAsync_ShouldLockUserAfterSixIncorrectCodes_WhenOtpIsRepeatedlyWrong()
    {
        const string otp = "Abc1234567";
        const string wrongOtp = "WrongCode1";
        var userId = Guid.NewGuid();
        var currentUser = CurrentUser();
        var otpKey = UserCacheKeys.GetDisableOtpKey(userId);
        var wrongTimesKey = UserCacheKeys.GetDisableOtpWrongTimesKey(userId);
        var lockKey = UserCacheKeys.GetDisableOtpLockKey(userId);
        var wrongTimes = 0;
        var isLocked = false;
        TimeSpan? lockExpiration = null;
        _userQueryService.Setup(service => service.GetCurrentUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);
        _cacheService.Setup(cache => cache.GetAsync<bool>(lockKey.Key, It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(isLocked));
        _cacheService.Setup(cache => cache.GetAsync<UserActionOtpChallengeModel>(otpKey.Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserActionOtpChallengeModel
            {
                OtpHash = Sha256Utils.ComputeHexHash(otp),
                RequestedById = currentUser.Id
            });
        _cacheService.Setup(cache => cache.GetAsync<int>(wrongTimesKey.Key, It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(wrongTimes));
        _cacheService.Setup(cache => cache.SetAsync(
                wrongTimesKey.Key, It.IsAny<int>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Callback<string, int, TimeSpan?, CancellationToken>((_, count, _, _) => wrongTimes = count)
            .Returns(Task.CompletedTask);
        _cacheService.Setup(cache => cache.SetAsync(
                lockKey.Key, true, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Callback<string, bool, TimeSpan?, CancellationToken>((_, _, expiration, _) =>
            {
                isLocked = true;
                lockExpiration = expiration;
            })
            .Returns(Task.CompletedTask);
        var service = CreateService();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            Func<Task> invalidAttempt = () => service.ConfirmDisableUserAsync(new DisableUserConfirmationRequestModel
            {
                UserId = userId,
                Otp = wrongOtp
            });
            await invalidAttempt.Should().ThrowAsync<UnprocessableEntityException>();
        }

        Func<Task> sixthAttempt = () => service.ConfirmDisableUserAsync(new DisableUserConfirmationRequestModel
        {
            UserId = userId,
            Otp = wrongOtp
        });
        var exception = (await sixthAttempt.Should().ThrowAsync<ForbiddenException>()).Which;

        exception.Message.Should().Be("You do not have permission to disable user.");
        wrongTimes.Should().Be(6);
        isLocked.Should().BeTrue();
        lockExpiration!.Value.TotalMinutes.Should().BeInRange(61, 90);

        Func<Task> lockedAttempt = () => service.ConfirmDisableUserAsync(new DisableUserConfirmationRequestModel
        {
            UserId = userId,
            Otp = otp
        });
        var lockedException = (await lockedAttempt.Should().ThrowAsync<ForbiddenException>()).Which;
        lockedException.Message.Should().Be("You do not have permission to disable user.");
    }

    [Fact]
    public async Task ChangeRoleAsync_ShouldBindRequestedRoleToCachedOtp_WhenRequestSucceeds()
    {
        var currentUser = CurrentUser();
        var user = new User { Id = Guid.NewGuid(), Username = "alice", DisplayName = "Alice" };
        var currentAssignment = new UserRole { UserId = user.Id, RoleId = Guid.NewGuid() };
        var currentRole = new Role { Id = currentAssignment.RoleId, Code = RC.Employee, DisplayName = "Employee" };
        var newRole = new Role { Id = Guid.NewGuid(), Code = RC.Admin, DisplayName = "Admin" };
        UserActionOtpChallengeModel? savedChallenge = null;
        OutboxEvent<SendNotificationEventModel>? publishedEvent = null;
        _userQueryService.Setup(service => service.GetCurrentUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);
        _userQueryService.Setup(service => service.IsSuperAdminAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepository.Setup(repository => repository.FindFirstByConditionAsync(
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRoleRepository.Setup(repository => repository.FindFirstByConditionAsync(
                It.IsAny<Func<IQueryable<UserRole>, IQueryable<UserRole>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentAssignment);
        _roleRepository.SetupSequence(repository => repository.FindFirstByConditionAsync(
                It.IsAny<Func<IQueryable<Role>, IQueryable<Role>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentRole)
            .ReturnsAsync(newRole);
        _cacheService.Setup(cache => cache.SetAsync(
                UserCacheKeys.GetChangeRoleOtpKey(user.Id).Key,
                It.IsAny<UserActionOtpChallengeModel>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Callback<string, UserActionOtpChallengeModel, TimeSpan?, CancellationToken>(
                (_, challenge, _, _) => savedChallenge = challenge)
            .Returns(Task.CompletedTask);
        _messagingPublisher.Setup(publisher => publisher.HandleAsync(
                It.IsAny<OutboxEvent<SendNotificationEventModel>>(), It.IsAny<CancellationToken>()))
            .Callback<OutboxEvent<SendNotificationEventModel>, CancellationToken>((message, _) => publishedEvent = message)
            .Returns(Task.CompletedTask);

        var result = await CreateService().ChangeRoleAsync(new ChangeRoleRequestModel
        {
            UserId = user.Id,
            RoleCode = RC.Admin
        });

        result.RoleCode.Should().Be(RC.Admin);
        savedChallenge.Should().NotBeNull();
        savedChallenge!.RoleCode.Should().Be(RC.Admin);
        savedChallenge.RequestedById.Should().Be(currentUser.Id);
        var email = publishedEvent!.Data.Email!;
        email.TemplateKey.Should().Be("ChangeRole");
        var emailData = email.Data!.RootElement;
        emailData.GetProperty("newRole").GetString().Should().Be("Admin");
        savedChallenge!.OtpHash.Should().Be(Sha256Utils.ComputeHexHash(
            emailData.GetProperty("otpCode").GetString()!));
    }

    [Fact]
    public async Task ConfirmChangeRoleAsync_ShouldCloseCurrentRoleAndReactivateRequestedRole_WhenOtpMatches()
    {
        const string otp = "Abc1234567";
        var currentUser = CurrentUser();
        var user = new User { Id = Guid.NewGuid(), IdentitySubject = "target-subject" };
        var currentAssignment = new UserRole
        {
            Id = Guid.NewGuid(), UserId = user.Id, RoleId = Guid.NewGuid(), EffectiveFrom = DateTimeOffset.UtcNow.AddDays(-10)
        };
        var oldRequestedAssignment = new UserRole
        {
            Id = Guid.NewGuid(), UserId = user.Id, RoleId = Guid.NewGuid(),
            EffectiveFrom = DateTimeOffset.UtcNow.AddDays(-20), EffectiveTo = DateTimeOffset.UtcNow.AddDays(-10)
        };
        var currentRole = new Role { Id = currentAssignment.RoleId, Code = RC.Employee };
        var requestedRole = new Role { Id = oldRequestedAssignment.RoleId, Code = RC.Admin };
        var otpKey = UserCacheKeys.GetChangeRoleOtpKey(user.Id);
        _userQueryService.Setup(service => service.GetCurrentUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);
        _userQueryService.Setup(service => service.IsSuperAdminAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _cacheService.Setup(cache => cache.GetAsync<UserActionOtpChallengeModel>(otpKey.Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserActionOtpChallengeModel
            {
                OtpHash = Sha256Utils.ComputeHexHash(otp),
                RequestedById = currentUser.Id,
                RoleCode = RC.Admin
            });
        _userRepository.Setup(repository => repository.FindFirstByConditionAsync(
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRoleRepository.SetupSequence(repository => repository.FindFirstByConditionAsync(
                It.IsAny<Func<IQueryable<UserRole>, IQueryable<UserRole>>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentAssignment)
            .ReturnsAsync(oldRequestedAssignment);
        _roleRepository.SetupSequence(repository => repository.FindFirstByConditionAsync(
                It.IsAny<Func<IQueryable<Role>, IQueryable<Role>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentRole)
            .ReturnsAsync(requestedRole);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(
                currentUser.Id.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await CreateService().ConfirmChangeRoleAsync(new ChangeRoleConfirmationRequestModel
        {
            UserId = user.Id,
            Otp = otp
        });

        currentAssignment.EffectiveTo.Should().NotBeNull();
        oldRequestedAssignment.EffectiveFrom.Should().BeAfter(currentAssignment.EffectiveFrom);
        oldRequestedAssignment.EffectiveTo.Should().BeNull();
        result.RoleCode.Should().Be(RC.Admin);
        _userRoleRepository.Verify(repository => repository.Add(It.IsAny<UserRole>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(
            currentUser.Id.ToString(), It.IsAny<CancellationToken>()), Times.Once);
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
        Id = Guid.NewGuid(), IdentitySubject = "creator-subject", DisplayName = "Creator", Email = "creator@example.test"
    };

    private static CreateUserRequestModel NewCreateRequest() => new()
    {
        IdentitySubject = "new-subject", Username = "newuser", Email = "new@example.test",
        FirstName = "New", LastName = "User", DisplayName = "New User", EmployeeCode = "E123",
        RoleCode = "employee", Gender = UserGender.Female, BirthDate = new DateOnly(1995, 5, 20)
    };
}
