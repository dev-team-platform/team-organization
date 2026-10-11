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
            ?? throw new NotFoundException($"Role with code '{model.RoleCode}' not found");

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

    public async Task UpdateLastLoginAsync(UpdateLastLoginRequestModel model, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindFirstByConditionAsync(
            q => q.Where(x => x.IdentitySubject == model.IdentitySubject),
            trackChanges: true,
            cancellationToken)
            ?? throw new NotFoundException("User not found");

        user.LastLoginAt = model.LastLoginAt;

        var currentUserKey = UserCacheKeys.GetCurrentUserKey(model.IdentitySubject);
        await _cacheService.RemoveAsync(currentUserKey.Key, cancellationToken);

        await _unitOfWork.SaveChangesWithoutAuditAsync(cancellationToken);
    }

}
