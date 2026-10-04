using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Entities;

namespace TeamOrganization.Application.Interfaces.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    public Task<GetCurrentUserResponseModel?> FindCurrentUserByIdentitySubjectAsync(
        string identitySubject,
        CancellationToken cancellationToken = default);

}
