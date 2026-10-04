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
        var now = DateTimeOffset.UtcNow;
        var query = from u in _dbContext.Set<User>()
                    join ur in _dbContext.Set<UserRole>() on u.Id equals ur.UserId
                    join r in _dbContext.Set<Role>() on ur.RoleId equals r.Id
                    join rp in _dbContext.Set<RolePermission>()
                        on r.Id equals rp.RoleId into rolePermissions
                    from rp in rolePermissions
                        .Where(rolePermission => ur.EffectiveFrom <= now
                            && (ur.EffectiveTo == null || ur.EffectiveTo > now))
                        .DefaultIfEmpty()
                    join p in _dbContext.Set<Permission>()
                        on rp.PermissionId equals p.Id into permissions
                    from p in permissions.DefaultIfEmpty()
                    where u.IdentitySubject == identitySubject
                    select new
                    {
                        User = u,
                        RoleName = r.DisplayName,
                        PermissionCode = p == null ? null : p.Code
                    };

        var results = await query.ToListAsync(cancellationToken);

        if (results.Count == 0)
        {
            return null;
        }

        var result = results[0];

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
            RoleName = result.RoleName,
            PermissionCodes =
            [
                .. results
                    .Where(x => x.PermissionCode is not null)
                    .Select(x => x.PermissionCode!)
                    .Distinct()
            ]
        };
    }
}
