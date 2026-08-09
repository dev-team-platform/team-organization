using TeamOrganization.Application.CacheKeys;
using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Application.Interfaces.Services.Cache;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Domain.Exceptions;

namespace TeamOrganization.Application.Services.Users;

public class UserCommandService : IUserCommandService
{
    private readonly Serilog.ILogger _logger;
    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;

    public UserCommandService(
        Serilog.ILogger logger,
        IUserRepository userRepository,
        ICacheService cacheService,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _userRepository = userRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateUserResponseModel> CreateUserAsync(CreateUserRequestModel model, CancellationToken cancellationToken = default)
    {
        _logger.Information("Creating new user");

        var newUser = new User
        {
            Id = Guid.CreateVersion7(),
            IdentitySubject = model.IdentitySubject,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            EmployeeCode = model.EmployeeCode
        };

        // _userRepository.Add(newUser);
        // await _unitOfWork.SaveChangesAsync(cancellationToken);

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