using System.Text.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Domain.Enums.Users;
using TeamOrganization.Infrastructure.Persistence;
using TeamOrganization.Infrastructure.Persistence.Interceptors;
using Xunit;

namespace TeamOrganization.Infrastructure.Tests.Persistence.Interceptors;

public class AuditSaveChangesInterceptorTests
{
    [Fact]
    public void SerializeValues_ShouldWriteEnumNames_WhenEnumPropertiesChange()
    {
        using var context = CreateContext();
        var user = new User { Id = Guid.NewGuid(), Status = UserStatus.Active, Username = "before" };
        context.Users.Attach(user);
        user.Status = UserStatus.Inactive;
        user.Username = "after";
        context.ChangeTracker.DetectChanges();
        var entry = context.Entry<EntityBase>(user);

        using var oldValues = JsonDocument.Parse(AuditSaveChangesInterceptor.SerializeValues(entry, true)!);
        using var newValues = JsonDocument.Parse(AuditSaveChangesInterceptor.SerializeValues(entry, false)!);

        oldValues.RootElement.GetProperty(nameof(User.Status)).GetString().Should().Be("Active");
        newValues.RootElement.GetProperty(nameof(User.Status)).GetString().Should().Be("Inactive");
        oldValues.RootElement.GetProperty(nameof(User.Username)).GetString().Should().Be("before");
        newValues.RootElement.GetProperty(nameof(User.Username)).GetString().Should().Be("after");
        newValues.RootElement.TryGetProperty(nameof(User.Gender), out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(EntityState.Added, false)]
    [InlineData(EntityState.Deleted, true)]
    public void SerializeValues_ShouldWriteEnumNames_WhenEntityIsAddedOrDeleted(EntityState state, bool useOriginalValues)
    {
        using var context = CreateContext();
        var user = new User { Id = Guid.NewGuid(), Status = UserStatus.Pending, LastName = null };
        var entry = context.Entry<EntityBase>(user);
        entry.State = state;

        using var values = JsonDocument.Parse(AuditSaveChangesInterceptor.SerializeValues(entry, useOriginalValues)!);

        values.RootElement.GetProperty(nameof(User.Status)).GetString().Should().Be("Pending");
        values.RootElement.GetProperty(nameof(User.LastName)).ValueKind.Should().Be(JsonValueKind.Null);
    }

    private static AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql("Host=localhost;Database=team-organization-test;Username=test;Password=test")
        .Options);
}
