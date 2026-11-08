using Microsoft.EntityFrameworkCore;
using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Application.Models.Common;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Infrastructure.Persistence;
using TeamOrganization.Infrastructure.Persistence.Extensions;

namespace TeamOrganization.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public async Task<bool> FindCanExportUsersAsync(string identitySubject, CancellationToken cancellationToken = default)
    {
        var query = BuildCanExportUsersQuery(_dbContext.Set<User>(), _dbContext.Set<UserRole>(),
            _dbContext.Set<Role>(), _dbContext.Set<RolePermission>(), _dbContext.Set<Permission>(), identitySubject, DateTimeOffset.UtcNow);
        return await query.AnyAsync(cancellationToken);
    }

    internal static IQueryable<Guid> BuildCanExportUsersQuery(
        IQueryable<User> users, IQueryable<UserRole> userRoles, IQueryable<Role> roles,
        IQueryable<RolePermission> rolePermissions, IQueryable<Permission> permissions,
        string identitySubject, DateTimeOffset now)
    {
        return from user in users
               join userRole in userRoles on user.Id equals userRole.UserId
               join role in roles on userRole.RoleId equals role.Id
               join rolePermission in rolePermissions on role.Id equals rolePermission.RoleId
               join permission in permissions on rolePermission.PermissionId equals permission.Id
               where user.IdentitySubject == identitySubject
                   && (role.Code == TeamOrganization.Domain.Constants.Uam.RC.Admin || role.Code == TeamOrganization.Domain.Constants.Uam.RC.SuperAdmin)
                   && permission.Code == TeamOrganization.Domain.Constants.Uam.PC.User.Export
                   && userRole.EffectiveFrom <= now
                   && (userRole.EffectiveTo == null || userRole.EffectiveTo > now)
               select user.Id;
    }
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
                    join ur in _dbContext.Set<UserRole>()
                        on u.Id equals ur.UserId into userRoles
                    from ur in userRoles.DefaultIfEmpty()
                    join r in _dbContext.Set<Role>()
                        on ur.RoleId equals r.Id into roles
                    from r in roles.DefaultIfEmpty()
                    join rp in _dbContext.Set<RolePermission>()
                        on r.Id equals rp.RoleId into rolePermissions
                    from rp in rolePermissions
                    join p in _dbContext.Set<Permission>()
                        on rp.PermissionId equals p.Id into permissions
                    from p in permissions.DefaultIfEmpty()
                    where u.IdentitySubject == identitySubject && ur.EffectiveFrom <= now
                            && (ur.EffectiveTo == null || ur.EffectiveTo > now)
                    select new
                    {
                        User = u,
                        RoleCode = r.Code,
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
            IdentitySubject = result.User.IdentitySubject,
            EmployeeCode = result.User.EmployeeCode,
            Username = result.User.Username,
            Email = result.User.Email,
            FirstName = result.User.FirstName,
            LastName = result.User.LastName,
            DisplayName = result.User.DisplayName,
            AvatarUrl = result.User.AvatarUrl,
            Gender = result.User.Gender,
            BirthDate = result.User.BirthDate,
            LastLoginAt = result.User.LastLoginAt,
            RoleName = result.RoleName,
            RoleCode = result.RoleCode,
            PermissionCodes =
            [
                .. results
                    .Where(x => x.PermissionCode is not null)
                    .Select(x => x.PermissionCode!)
                    .Distinct()
            ]
        };
    }

    public async Task<FilterResult<GetAllUsersResponseModelItem>> FindAllUsersAsync(
        FilterQuery<GetAllUsersResponseModelItem> query,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var users = from user in _dbContext.Set<User>()
                    join ur in _dbContext.Set<UserRole>()
                        on user.Id equals ur.UserId into userRoles
                    from ur in userRoles.DefaultIfEmpty()
                    join r in _dbContext.Set<Role>()
                        on ur.RoleId equals r.Id into roles
                    from r in roles.DefaultIfEmpty()
                    join createdByUser in _dbContext.Set<User>()
                        on user.CreatedById equals createdByUser.Id into createdByUsers
                    from createdByUser in createdByUsers.DefaultIfEmpty()
                    join updatedByUser in _dbContext.Set<User>()
                        on user.UpdatedById equals updatedByUser.Id into updatedByUsers
                    from updatedByUser in updatedByUsers.DefaultIfEmpty()
                    where ur == null || (ur.EffectiveFrom <= now
                        && (ur.EffectiveTo == null || ur.EffectiveTo > now))
                    select new GetAllUsersResponseModelItem
                    {
                        Id = user.Id,
                        EmployeeCode = user.EmployeeCode,
                        Username = user.Username,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        DisplayName = user.DisplayName,
                        RoleName = r.DisplayName,
                        AvatarUrl = user.AvatarUrl,
                        Status = user.Status,
                        Gender = user.Gender,
                        BirthDate = user.BirthDate,
                        LastLoginAt = user.LastLoginAt,
                        IsPendingDisabled = user.AdditionalData.DisableOtp != null
                            && user.AdditionalData.DisableOtpExpired != null
                            && user.AdditionalData.DisableOtpExpired > now,
                        IsPendingChangedRole = user.AdditionalData.ChangeRoleOtp != null
                            && user.AdditionalData.ChangeRoleOtpExpired != null
                            && user.AdditionalData.ChangeRoleOtpExpired > now,
                        CreatedAt = user.CreatedAt,
                        CreatedBy = createdByUser == null
                            ? null
                            : new NavigationResponseModel
                            {
                                Id = createdByUser.Id,
                                Name = createdByUser.DisplayName
                            },
                        UpdatedAt = user.UpdatedAt,
                        UpdatedBy = updatedByUser == null
                            ? null
                            : new NavigationResponseModel
                            {
                                Id = updatedByUser.Id,
                                Name = updatedByUser.DisplayName
                            }
                    };

        if (query.SortFields.Count == 0)
        {
            query.SortFields.Add(new SortField
            {
                FieldName = "displayName",
                IsAscending = true
            });
        }

        if (!query.SortFields.Any(sortField =>
                string.Equals(sortField.FieldName, "id", StringComparison.OrdinalIgnoreCase)))
        {
            query.SortFields.Add(new SortField
            {
                FieldName = "id",
                IsAscending = true
            });
        }

        return await users.AsNoTracking().ToFilterResultAsync(query, cancellationToken);
    }
}
