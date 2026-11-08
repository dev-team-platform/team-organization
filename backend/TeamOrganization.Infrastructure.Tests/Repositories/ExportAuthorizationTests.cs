using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Infrastructure.Persistence;
using TeamOrganization.Infrastructure.Repositories;
using Xunit;

namespace TeamOrganization.Infrastructure.Tests.Repositories;

public class ExportAuthorizationTests
{
    [Theory]
    [InlineData("ADMIN", "user.export", false, true)]
    [InlineData("SUPER_ADMIN", "user.export", false, true)]
    [InlineData("EMPLOYEE", "user.export", false, false)]
    [InlineData("ADMIN", "user.read", false, false)]
    [InlineData("ADMIN", "user.export", true, false)]
    public void BuildCanExportUsersQuery_ShouldRequireEligibleActiveRoleAndPermission_WhenCheckingAccess(string roleCode, string permissionCode, bool expired, bool allowed)
    {
        var now = DateTimeOffset.UtcNow;
        var user = new User { Id = Guid.NewGuid(), IdentitySubject = "subject" };
        var role = new Role { Id = Guid.NewGuid(), Code = roleCode };
        var permission = new Permission { Id = Guid.NewGuid(), Code = permissionCode };
        var assignment = new UserRole { UserId = user.Id, RoleId = role.Id, EffectiveFrom = now.AddDays(-2), EffectiveTo = expired ? now.AddDays(-1) : null };
        var rolePermission = new RolePermission { RoleId = role.Id, PermissionId = permission.Id };

        var result = UserRepository.BuildCanExportUsersQuery(new[] { user }.AsQueryable(), new[] { assignment }.AsQueryable(),
            new[] { role }.AsQueryable(), new[] { rolePermission }.AsQueryable(), new[] { permission }.AsQueryable(), "subject", now).Any();

        result.Should().Be(allowed);
    }

    [Fact]
    public void BuildCanExportUsersQuery_ShouldTranslateToSql_WhenUsingPostgres()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=localhost;Database=test;Username=test;Password=test").Options;
        using var context = new AppDbContext(options);
        var sql = UserRepository.BuildCanExportUsersQuery(context.Users, context.UserRoles, context.Roles, context.RolePermissions, context.Permissions, "subject", DateTimeOffset.UtcNow).ToQueryString();
        sql.Should().Contain("JOIN").And.Contain("effective_from").And.Contain("effective_to").And.Contain("user.export");
    }
}
