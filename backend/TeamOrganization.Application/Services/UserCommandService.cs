using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Entities;

namespace TeamOrganization.Application.Services;

public class UserCommandService : IUserCommandService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserCommandService(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateUserResponseModel> CreateUserAsync(CreateUserRequestModel model, CancellationToken cancellationToken = default)
    {
        var newUser = new User
        {
            Id = Guid.CreateVersion7(),
            IdentitySubject = model.IdentitySubject,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            DisplayName = model.DisplayName,
            EmployeeCode = model.EmployeeCode
        };

        _userRepository.Add(newUser);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateUserResponseModel
        {
            Id = newUser.Id
        };
    }
}