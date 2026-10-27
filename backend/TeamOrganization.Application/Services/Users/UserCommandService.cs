using System.Text.Json;
using TeamOrganization.Application.Interfaces.Contexts;
using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Application.Interfaces.Services.Cache;
using TeamOrganization.Application.Interfaces.Services.Messaging;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Application.Models.Common;
using TeamOrganization.Application.Models.Messaging.Publishing;
using TeamOrganization.Application.Models.Messaging.Publishing.Notifications;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Constants.CacheKeys;
using TeamOrganization.Domain.Constants.EventRoutingKeys;
using TeamOrganization.Domain.Constants.Notifications;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Domain.Enums.Users;
using TeamOrganization.Domain.Exceptions;
using TeamOrganization.Domain.Utils;

namespace TeamOrganization.Application.Services.Users;

public class UserCommandService : IUserCommandService
{
    private readonly Serilog.ILogger _logger;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUserQueryService _userQueryService;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessagingPublisher<OutboxEvent<SendNotificationEventModel>> _messagingPublisher;

    public UserCommandService(
        Serilog.ILogger logger,
        ICurrentUserContext currentUserContext,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        IUserQueryService userQueryService,
        ICacheService cacheService,
        IUnitOfWork unitOfWork,
        IMessagingPublisher<OutboxEvent<SendNotificationEventModel>> messagingPublisher)
    {
        _logger = logger;
        _currentUserContext = currentUserContext;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _userQueryService = userQueryService;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
        _messagingPublisher = messagingPublisher;
    }

    public async Task<CreateUserResponseModel> CreateNewUserAsync(CreateUserRequestModel model, CancellationToken cancellationToken = default)
    {
        var creator = await _userQueryService.GetCurrentUserAsync(cancellationToken);

        var role = await _roleRepository.FindFirstByConditionAsync(
            q => q.Where(x => x.Code == model.RoleCode),
            cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Role with code '{model.RoleCode}' not found.");

        var newUser = new User
        {
            Id = Guid.CreateVersion7(),
            IdentitySubject = model.IdentitySubject,
            Username = model.Username,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            DisplayName = model.DisplayName,
            EmployeeCode = model.EmployeeCode,
            Gender = model.Gender,
            BirthDate = model.BirthDate,
            Status = UserStatus.Active
        };

        _userRepository.Add(newUser);

        var newUserRole = new UserRole
        {
            Id = Guid.CreateVersion7(),
            UserId = newUser.Id,
            RoleId = role.Id,
            EffectiveFrom = DateTimeOffset.UtcNow
        };

        _userRoleRepository.Add(newUserRole);

        var eventData = CreatedUserEventData.FromEntity(newUser);
        var notificationEvent = new OutboxEvent<SendNotificationEventModel>
        {
            Type = EventRoutingKeys.UserCreated,
            RoutingKey = EventRoutingKeys.UserCreated,
            Data = new SendNotificationEventModel
            {
                EventType = EventRoutingKeys.UserCreated,
                CreatedById = creator.Id,
                CreatedAt = DateTimeOffset.UtcNow,
                Notification = new NotificationEventModel
                {
                    Title = NotificationContents.CreatedUser.Title,
                    Message = NotificationContents.CreatedUser.SuccessMessage,
                    Data = JsonSerializer.SerializeToDocument(eventData, JsonUtils.WebSerializerOptions),
                    Recipients =
                    [
                        new NotificationRecipientEventModel
                        {
                            UserId = creator.Id,
                            IdentitySubject = creator.IdentitySubject
                        }
                    ]
                },
            }
        };

        await _unitOfWork.SaveChangesAsync(creator.Id.ToString(), cancellationToken);

        // The user is already committed, so cancellation of the notification publish must not
        // make the API compensate by deleting the Keycloak account while leaving the DB user.
        try
        {
            await _messagingPublisher.HandleAsync(notificationEvent, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.Warning(
                "Notification publishing was cancelled after user {UserId} was committed",
                newUser.Id);
        }

        return new CreateUserResponseModel
        {
            Id = newUser.Id,
            IdentitySubject = newUser.IdentitySubject,
            Username = newUser.Username,
            Email = newUser.Email,
            FirstName = newUser.FirstName,
            LastName = newUser.LastName,
            DisplayName = newUser.DisplayName,
            EmployeeCode = newUser.EmployeeCode,
            Gender = newUser.Gender,
            BirthDate = newUser.BirthDate,
            CreatedBy = new NavigationResponseModel
            {
                Id = creator.Id,
                Name = creator.DisplayName
            },
            UpdatedBy = new NavigationResponseModel
            {
                Id = creator.Id,
                Name = creator.DisplayName
            }
        };
    }

    public async Task<UpdateUserResponseModel> UpdateUserAsync(UpdateUserRequestModel model, CancellationToken cancellationToken = default)
    {
        var currentUser = await _userQueryService.GetCurrentUserAsync(cancellationToken);

        var user = await _userRepository.FindFirstByConditionAsync(
            q => q.Where(x => x.Id == model.UserId),
            trackChanges: true,
            cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (await _userQueryService.IsSuperAdminAsync(user.Id, cancellationToken))
        {
            throw new ForbiddenException("You do not have permission to update this user");
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.DisplayName = model.DisplayName;
        user.EmployeeCode = model.EmployeeCode;
        user.Gender = model.Gender;
        user.BirthDate = model.BirthDate;

        await _unitOfWork.SaveChangesAsync(currentUser.Id.ToString(), cancellationToken);

        var currentUserKey = UserCacheKeys.GetCurrentUserKey(user.IdentitySubject);
        await _cacheService.RemoveAsync(currentUserKey.Key, cancellationToken);

        return new UpdateUserResponseModel
        {
            Id = user.Id,
            IdentitySubject = user.IdentitySubject,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            EmployeeCode = user.EmployeeCode,
            Gender = user.Gender,
            BirthDate = user.BirthDate,
            UpdatedBy = new NavigationResponseModel
            {
                Id = currentUser.Id,
                Name = currentUser.DisplayName
            }
        };
    }

    public async Task<DisableUserResponseModel> DisableUserAsync(
        DisableUserRequestModel model,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userQueryService.GetCurrentUserAsync(cancellationToken);
        var lockKey = UserCacheKeys.GetDisableOtpLockKey(model.UserId);
        await ThrowIfLockedAsync(lockKey, "You do not have permission to disable user.", cancellationToken);

        var user = await FindUserAsync(model.UserId, trackChanges: false, cancellationToken);
        await ThrowIfSuperAdminAsync(user, "You do not have permission to disable user.", cancellationToken);
        if (user.Status == UserStatus.Inactive)
        {
            throw new ConflictException("User is already disabled.");
        }

        var otp = OtpUtils.GenerateOtp();
        var otpKey = UserCacheKeys.GetDisableOtpKey(user.Id);
        await _cacheService.SetAsync(
            otpKey.Key,
            new UserActionOtpChallengeModel
            {
                OtpHash = Sha256Utils.ComputeHexHash(otp),
                RequestedById = currentUser.Id
            },
            otpKey.Expiration,
            cancellationToken);

        var emailData = new DisableUserOtpEmailData
        {
            RecipientDisplayName = currentUser.DisplayName,
            TargetDisplayName = user.DisplayName,
            TargetUsername = user.Username,
            CurrentStatus = user.Status.ToString(),
            OtpCode = otp,
            OtpExpiryMinutes = UserCacheKeys.OtpExpirationMinutes,
            CurrentYear = DateTime.UtcNow.Year
        };

        await PublishOtpEmailAsync(
            EventRoutingKeys.UserDisableOtpRequested,
            EmailTemplateKeys.DisableUser,
            currentUser,
            JsonSerializer.SerializeToDocument(emailData, JsonUtils.WebSerializerOptions),
            cancellationToken);

        return new DisableUserResponseModel
        {
            UserId = user.Id,
            OtpExpiresInMinutes = UserCacheKeys.OtpExpirationMinutes
        };
    }

    public async Task<DisableUserConfirmationResponseModel> ConfirmDisableUserAsync(
        DisableUserConfirmationRequestModel model,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userQueryService.GetCurrentUserAsync(cancellationToken);
        var otpKey = UserCacheKeys.GetDisableOtpKey(model.UserId);
        var wrongTimesKey = UserCacheKeys.GetDisableOtpWrongTimesKey(model.UserId);
        var lockKey = UserCacheKeys.GetDisableOtpLockKey(model.UserId);

        var challenge = await VerifyOtpAsync(
            model.Otp,
            otpKey,
            wrongTimesKey,
            lockKey,
            "You do not have permission to disable user.",
            cancellationToken);

        if (challenge.RequestedById != currentUser.Id)
        {
            throw new ForbiddenException("You do not have permission to disable user.");
        }

        var user = await FindUserAsync(model.UserId, trackChanges: true, cancellationToken);
        await ThrowIfSuperAdminAsync(user, "You do not have permission to disable user.", cancellationToken);
        if (user.Status == UserStatus.Inactive)
        {
            throw new ConflictException("User is already disabled.");
        }

        user.Status = UserStatus.Inactive;
        await _unitOfWork.SaveChangesAsync(currentUser.Id.ToString(), cancellationToken);
        await _cacheService.RemoveAsync(UserCacheKeys.GetCurrentUserKey(user.IdentitySubject).Key, cancellationToken);
        await ClearOtpAsync(otpKey, wrongTimesKey, cancellationToken);

        return new DisableUserConfirmationResponseModel
        {
            UserId = user.Id,
            Status = user.Status,
            UpdatedBy = new NavigationResponseModel
            {
                Id = currentUser.Id,
                Name = currentUser.DisplayName
            }
        };
    }

    public async Task<ChangeRoleResponseModel> ChangeRoleAsync(
        ChangeRoleRequestModel model,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userQueryService.GetCurrentUserAsync(cancellationToken);
        var lockKey = UserCacheKeys.GetChangeRoleOtpLockKey(model.UserId);
        await ThrowIfLockedAsync(lockKey, "You do not have permission to change role.", cancellationToken);

        var user = await FindUserAsync(model.UserId, trackChanges: false, cancellationToken);
        await ThrowIfSuperAdminAsync(user, "You do not have permission to change role.", cancellationToken);

        var currentAssignment = await FindCurrentUserRoleAsync(user.Id, trackChanges: false, cancellationToken);
        var currentRole = await FindRoleAsync(currentAssignment.RoleId, cancellationToken);
        var newRole = await _roleRepository.FindFirstByConditionAsync(
            q => q.Where(x => x.Code == model.RoleCode),
            cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Role with code '{model.RoleCode}' not found");

        if (currentRole.Id == newRole.Id)
        {
            throw new ConflictException("User already has the requested role.");
        }

        var otp = OtpUtils.GenerateOtp();
        var otpKey = UserCacheKeys.GetChangeRoleOtpKey(user.Id);
        await _cacheService.SetAsync(
            otpKey.Key,
            new UserActionOtpChallengeModel
            {
                OtpHash = Sha256Utils.ComputeHexHash(otp),
                RequestedById = currentUser.Id,
                RoleCode = newRole.Code
            },
            otpKey.Expiration,
            cancellationToken);

        var emailData = new ChangeRoleOtpEmailData
        {
            RecipientDisplayName = currentUser.DisplayName,
            TargetDisplayName = user.DisplayName,
            TargetUsername = user.Username,
            CurrentRole = currentRole.DisplayName,
            NewRole = newRole.DisplayName,
            OtpCode = otp,
            OtpExpiryMinutes = UserCacheKeys.OtpExpirationMinutes,
            CurrentYear = DateTime.UtcNow.Year
        };

        await PublishOtpEmailAsync(
            EventRoutingKeys.UserChangeRoleOtpRequested,
            EmailTemplateKeys.ChangeRole,
            currentUser,
            JsonSerializer.SerializeToDocument(emailData, JsonUtils.WebSerializerOptions),
            cancellationToken);

        return new ChangeRoleResponseModel
        {
            UserId = user.Id,
            RoleCode = newRole.Code,
            OtpExpiresInMinutes = UserCacheKeys.OtpExpirationMinutes
        };
    }

    public async Task<ChangeRoleConfirmationResponseModel> ConfirmChangeRoleAsync(
        ChangeRoleConfirmationRequestModel model,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userQueryService.GetCurrentUserAsync(cancellationToken);
        var otpKey = UserCacheKeys.GetChangeRoleOtpKey(model.UserId);
        var wrongTimesKey = UserCacheKeys.GetChangeRoleOtpWrongTimesKey(model.UserId);
        var lockKey = UserCacheKeys.GetChangeRoleOtpLockKey(model.UserId);

        var challenge = await VerifyOtpAsync(
            model.Otp,
            otpKey,
            wrongTimesKey,
            lockKey,
            "You do not have permission to change role.",
            cancellationToken);

        if (string.IsNullOrWhiteSpace(challenge.RoleCode))
        {
            throw new UnprocessableEntityException("The role change OTP is invalid or expired.");
        }

        if (challenge.RequestedById != currentUser.Id)
        {
            throw new ForbiddenException("You do not have permission to change role.");
        }

        var user = await FindUserAsync(model.UserId, trackChanges: true, cancellationToken);
        await ThrowIfSuperAdminAsync(user, "You do not have permission to change role.", cancellationToken);

        var currentAssignment = await FindCurrentUserRoleAsync(user.Id, trackChanges: true, cancellationToken);
        var currentRole = await FindRoleAsync(currentAssignment.RoleId, cancellationToken);
        var newRole = await _roleRepository.FindFirstByConditionAsync(
            q => q.Where(x => x.Code == challenge.RoleCode),
            cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Role with code '{challenge.RoleCode}' not found");

        if (currentRole.Id == newRole.Id)
        {
            throw new ConflictException("The user's role has changed since the OTP was requested.");
        }

        var now = DateTimeOffset.UtcNow;
        currentAssignment.EffectiveTo = now;

        var existingAssignment = await _userRoleRepository.FindFirstByConditionAsync(
            q => q.Where(x => x.UserId == user.Id && x.RoleId == newRole.Id),
            trackChanges: true,
            cancellationToken);

        if (existingAssignment is null)
        {
            _userRoleRepository.Add(new UserRole
            {
                Id = Guid.CreateVersion7(),
                UserId = user.Id,
                RoleId = newRole.Id,
                EffectiveFrom = now,
                CreatedAt = now,
                CreatedById = currentUser.Id
            });
        }
        else
        {
            existingAssignment.EffectiveFrom = now;
            existingAssignment.EffectiveTo = null;
        }

        await _unitOfWork.SaveChangesAsync(currentUser.Id.ToString(), cancellationToken);
        await _cacheService.RemoveAsync(UserCacheKeys.GetCurrentUserKey(user.IdentitySubject).Key, cancellationToken);
        await ClearOtpAsync(otpKey, wrongTimesKey, cancellationToken);

        return new ChangeRoleConfirmationResponseModel
        {
            UserId = user.Id,
            RoleCode = newRole.Code,
            UpdatedBy = new NavigationResponseModel
            {
                Id = currentUser.Id,
                Name = currentUser.DisplayName
            }
        };
    }

    public async Task UpdateLastLoginAsync(UpdateLastLoginRequestModel model, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindFirstByConditionAsync(
            q => q.Where(x => x.IdentitySubject == model.IdentitySubject),
            trackChanges: true,
            cancellationToken)
            ?? throw new NotFoundException("User not found.");

        user.LastLoginAt = model.LastLoginAt;

        var currentUserKey = UserCacheKeys.GetCurrentUserKey(model.IdentitySubject);
        await _cacheService.RemoveAsync(currentUserKey.Key, cancellationToken);

        await _unitOfWork.SaveChangesWithoutAuditAsync(cancellationToken);
    }

    private async Task<User> FindUserAsync(Guid userId, bool trackChanges, CancellationToken cancellationToken)
    {
        return await _userRepository.FindFirstByConditionAsync(
            q => q.Where(x => x.Id == userId),
            trackChanges,
            cancellationToken)
            ?? throw new NotFoundException("User not found");
    }

    private async Task<UserRole> FindCurrentUserRoleAsync(
        Guid userId,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await _userRoleRepository.FindFirstByConditionAsync(
            q => q.Where(x => x.UserId == userId
                && x.EffectiveFrom <= now
                && (x.EffectiveTo == null || x.EffectiveTo > now)),
            trackChanges,
            cancellationToken)
            ?? throw new NotFoundException("Active user role not found.");
    }

    private async Task<Role> FindRoleAsync(Guid roleId, CancellationToken cancellationToken)
    {
        return await _roleRepository.FindFirstByConditionAsync(
            q => q.Where(x => x.Id == roleId),
            cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Role not found.");
    }

    private async Task ThrowIfSuperAdminAsync(User user, string message, CancellationToken cancellationToken)
    {
        if (await _userQueryService.IsSuperAdminAsync(user.Id, cancellationToken))
        {
            throw new ForbiddenException(message);
        }
    }

    private async Task ThrowIfLockedAsync(
        CacheKeyObject lockKey,
        string message,
        CancellationToken cancellationToken)
    {
        if (await _cacheService.GetAsync<bool>(lockKey.Key, cancellationToken))
        {
            throw new ForbiddenException(message);
        }
    }

    private async Task<UserActionOtpChallengeModel> VerifyOtpAsync(
        string otp,
        CacheKeyObject otpKey,
        CacheKeyObject wrongTimesKey,
        CacheKeyObject lockKey,
        string lockedMessage,
        CancellationToken cancellationToken)
    {
        await ThrowIfLockedAsync(lockKey, lockedMessage, cancellationToken);

        var challenge = await _cacheService.GetAsync<UserActionOtpChallengeModel>(otpKey.Key, cancellationToken);
        if (challenge is null)
        {
            throw new UnprocessableEntityException("The OTP is invalid or expired.");
        }

        var submittedHash = Sha256Utils.ComputeHexHash(otp);
        var hashMatches = Sha256Utils.Equals(submittedHash, challenge.OtpHash);

        if (!hashMatches)
        {
            var wrongTimes = await _cacheService.GetAsync<int>(wrongTimesKey.Key, cancellationToken);
            wrongTimes++;
            await _cacheService.SetAsync(wrongTimesKey.Key, wrongTimes, wrongTimesKey.Expiration, cancellationToken);

            if (wrongTimes > 5)
            {
                await _cacheService.SetAsync(lockKey.Key, true, lockKey.Expiration, cancellationToken);
                await _cacheService.RemoveAsync(otpKey.Key, cancellationToken);
                throw new ForbiddenException(lockedMessage);
            }

            throw new UnprocessableEntityException(
                "The OTP is incorrect.",
                new Dictionary<string, object?>()
                {
                    ["wrongAttemptsUntilLock"] = Math.Max(0, 5 - wrongTimes)
                }
            );
        }

        return challenge;
    }

    private async Task ClearOtpAsync(
        CacheKeyObject otpKey,
        CacheKeyObject wrongTimesKey,
        CancellationToken cancellationToken)
    {
        await _cacheService.RemoveAsync(otpKey.Key, cancellationToken);
        await _cacheService.RemoveAsync(wrongTimesKey.Key, cancellationToken);
    }

    private Task PublishOtpEmailAsync(
        string eventType,
        string templateKey,
        GetCurrentUserResponseModel currentUser,
        JsonDocument emailData,
        CancellationToken cancellationToken)
    {
        return _messagingPublisher.HandleAsync(
            new OutboxEvent<SendNotificationEventModel>
            {
                Type = eventType,
                RoutingKey = eventType,
                Data = new SendNotificationEventModel
                {
                    EventType = eventType,
                    CreatedById = currentUser.Id,
                    CreatedAt = DateTimeOffset.UtcNow,
                    Email = new EmailEventModel
                    {
                        TemplateKey = templateKey,
                        Recipients =
                        [
                            new EmailRecipientEventModel
                            {
                                UserId = currentUser.Id,
                                Email = currentUser.Email
                            }
                        ],
                        Data = emailData
                    }
                }
            },
            cancellationToken);
    }

}
