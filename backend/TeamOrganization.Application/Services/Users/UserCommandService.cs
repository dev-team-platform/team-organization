using TeamOrganization.Application.CacheKeys;
using TeamOrganization.Application.Interfaces.Contexts;
using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Application.Interfaces.Services.Cache;
using TeamOrganization.Application.Interfaces.Services.OutboxEvents;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Application.Models.Messaging;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Domain.Enums;
using TeamOrganization.Domain.Exceptions;

namespace TeamOrganization.Application.Services.Users;

public class UserCommandService : IUserCommandService
{
    private readonly Serilog.ILogger _logger;
    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IOutboxEventService _outboxEventService;

    public UserCommandService(
        Serilog.ILogger logger,
        IUserRepository userRepository,
        ICacheService cacheService,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUserContext,
        IOutboxEventService outboxEventService)
    {
        _logger = logger;
        _userRepository = userRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
        _currentUserContext = currentUserContext;
        _outboxEventService = outboxEventService;
    }

    public async Task<CreateUserResponseModel> CreateUserAsync(CreateUserRequestModel model, CancellationToken cancellationToken = default)
    {
        _logger.Information("Creating new user");

        var creatorIdentitySubject = _currentUserContext.IdentitySubject;
        var creator = await _userRepository.FindFirstByConditionAsync(
            query => query.Where(user => user.IdentitySubject == creatorIdentitySubject),
            cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Creating user was not found.");

        var newUser = new User
        {
            Id = Guid.CreateVersion7(),
            IdentitySubject = model.IdentitySubject,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            DisplayName = model.DisplayName,
            Username = model.Email.Split('@', 2)[0],
            EmployeeCode = model.EmployeeCode,
            Status = UserStatus.Active
        };

        _userRepository.Add(newUser);
        var userCreatedEvent = new UserCreatedIntegrationEvent
        {
            UserId = newUser.Id,
            IdentitySubject = newUser.IdentitySubject,
            DisplayName = newUser.DisplayName,
            Email = newUser.Email,
            IsActive = true,
            CreatedByUserId = creator.Id,
            CreatedByIdentitySubject = creatorIdentitySubject,
            OccurredAt = DateTimeOffset.UtcNow
        };

        await _unitOfWork.SaveChangesAsync(creator.Id, cancellationToken);

        var eventPublished = await _outboxEventService.PublishOutboxEventsAsync(
            userCreatedEvent.ToOutboxEvent(),
            cancellationToken);

        if (!eventPublished)
        {
            _logger.Warning(
                "User {UserId} was created, but its integration event was not published.",
                newUser.Id);
        }

        return new CreateUserResponseModel
        {
            Id = newUser.Id
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
