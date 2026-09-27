using System.Text.Json;
using TeamOrganization.Application.CacheKeys;
using TeamOrganization.Application.Constants.EventRoutingKeys;
using TeamOrganization.Application.Constants.Notifications;
using TeamOrganization.Application.Interfaces.Contexts;
using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Application.Interfaces.Services.Cache;
using TeamOrganization.Application.Interfaces.Services.OutboxEvents;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Application.Models.Common;
using TeamOrganization.Application.Models.OutboxEvents;
using TeamOrganization.Application.Models.OutboxEvents.Notifications;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Domain.Enums;
using TeamOrganization.Domain.Exceptions;
using TeamOrganization.Domain.Utils;

namespace TeamOrganization.Application.Services.Users;

public class UserCommandService : IUserCommandService
{
    private readonly Serilog.ILogger _logger;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IUserRepository _userRepository;
    private readonly IUserQueryService _userQueryService;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxEventService _outboxEventService;

    public UserCommandService(
        Serilog.ILogger logger,
        ICurrentUserContext currentUserContext,
        IUserRepository userRepository,
        IUserQueryService userQueryService,
        ICacheService cacheService,
        IUnitOfWork unitOfWork,
        IOutboxEventService outboxEventService)
    {
        _logger = logger;
        _currentUserContext = currentUserContext;
        _userRepository = userRepository;
        _userQueryService = userQueryService;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
        _outboxEventService = outboxEventService;
    }

    public async Task<CreateUserResponseModel> CreateNewUserAsync(CreateUserRequestModel model, CancellationToken cancellationToken = default)
    {
        var creator = await _userQueryService.GetCurrentUserAsync(cancellationToken);

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
            Status = UserStatus.Active
        };

        _userRepository.Add(newUser);

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

        await _unitOfWork.SaveChangesAsync(creator.Id, cancellationToken);

        await _outboxEventService.PublishAsync(notificationEvent, cancellationToken);

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

        await _unitOfWork.SaveChangesAsync(user.Id, cancellationToken);
    }

    public async Task UpdateLastLogoutAsync(UpdateLastLogoutRequestModel model, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindFirstByConditionAsync(
            q => q.Where(x => x.IdentitySubject == model.IdentitySubject),
            trackChanges: true,
            cancellationToken)
            ?? throw new NotFoundException("User not found");

        user.LastLogoutAt = model.LastLogoutAt;

        var currentUserKey = UserCacheKeys.GetCurrentUserKey(model.IdentitySubject);
        await _cacheService.RemoveAsync(currentUserKey.Key, cancellationToken);

        await _unitOfWork.SaveChangesAsync(user.Id, cancellationToken);
    }
}
