using Microsoft.EntityFrameworkCore;
using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Infrastructure.Persistence;

namespace TeamOrganization.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(Serilog.ILogger logger, AppDbContext appDbContext)
        : base(logger, appDbContext)
    {
    }

    public async Task<GetCurrentUserResponseModel?> FindCurrentUserByIdentitySubjectAsync(
        string identitySubject,
        CancellationToken cancellationToken = default)
    {
        var query = from u in _dbContext.Set<User>()
                    where u.IdentitySubject == identitySubject
                    join ur in _dbContext.Set<UserRole>() on u.Id equals ur.UserId
                    join r in _dbContext.Set<Role>() on ur.RoleId equals r.Id
                    select new
                    {
                        User = u,
                        Role = r,
                    };

        var result = await query.FirstOrDefaultAsync(cancellationToken);

        if (result is null)
        {
            return null;
        }

        return new GetCurrentUserResponseModel
        {
            Id = result.User.Id,
            EmployeeCode = result.User.EmployeeCode,
            Username = result.User.Username,
            Email = result.User.Email,
            FirstName = result.User.FirstName,
            LastName = result.User.LastName,
            DisplayName = result.User.DisplayName,
            AvatarUrl = result.User.AvatarUrl,
            LastLoginAt = result.User.LastLoginAt,
            LastLogoutAt = result.User.LastLogoutAt,
            RoleName = result.Role.DisplayName,
            PermissionCodes = []
        };
    }
}