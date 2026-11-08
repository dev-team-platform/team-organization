using System.Text.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Infrastructure.Persistence;
using TeamOrganization.Infrastructure.Persistence.Interceptors;
using Xunit;

namespace TeamOrganization.Infrastructure.Tests.Persistence;

public class UserAdditionalDataTests
{
    [Fact]
    public void UserAdditionalData_ShouldTranslateOtpExpiryAccess_WhenQueried()
    {
        using var context = CreateContext();
        var now = DateTimeOffset.UtcNow;

        var sql = context.Users
            .Where(user => user.AdditionalData.DisableOtp != null
                && user.AdditionalData.DisableOtpExpired != null
                && user.AdditionalData.DisableOtpExpired > now)
            .Select(user => user.Id)
            .ToQueryString();

        sql.Should().Contain("additional_data");
        sql.Should().Contain("disableOtp");
        sql.Should().Contain("disableOtpExpired");
    }

    [Fact]
    public void SerializeValues_ShouldCompareAdditionalDataContent_WhenJsonDataChanges()
    {
        var originalRequestedById = Guid.NewGuid();
        var updatedRequestedById = Guid.NewGuid();
        var originalExpiry = DateTimeOffset.Parse("2026-10-30T12:00:00+00:00");
        var user = new User
        {
            Id = Guid.NewGuid(),
            AdditionalData = new AdditionalDataObject
            {
                DisableOtp = "disable-otp-hash",
                DisableOtpExpired = originalExpiry,
                DisableOtpRequestedById = originalRequestedById,
                ChangeRoleOtp = "change-role-otp-hash"
            }
        };

        using var context = CreateContext();

        context.Users.Attach(user);
        user.AdditionalData = new AdditionalDataObject
        {
            DisableOtp = "disable-otp-hash",
            DisableOtpExpired = originalExpiry,
            DisableOtpRequestedById = originalRequestedById,
            ChangeRoleOtp = "change-role-otp-hash"
        };
        context.ChangeTracker.DetectChanges();
        context.Entry(user).State.Should().Be(EntityState.Unchanged);

        user.AdditionalData.DisableOtpRequestedById = updatedRequestedById;
        context.ChangeTracker.DetectChanges();
        var entry = context.Entry<EntityBase>(user);
        entry.State.Should().Be(EntityState.Modified);

        var oldValues = JsonDocument.Parse(AuditSaveChangesInterceptor.SerializeValues(entry, useOriginalValues: true)!);
        var newValues = JsonDocument.Parse(AuditSaveChangesInterceptor.SerializeValues(entry, useOriginalValues: false)!);
        var oldAdditionalData = oldValues.RootElement.GetProperty(nameof(User.AdditionalData));
        var newAdditionalData = newValues.RootElement.GetProperty(nameof(User.AdditionalData));

        oldAdditionalData
            .GetProperty(nameof(AdditionalDataObject.DisableOtpRequestedById))
            .GetGuid().Should().Be(originalRequestedById);
        newAdditionalData
            .GetProperty(nameof(AdditionalDataObject.DisableOtpRequestedById))
            .GetGuid().Should().Be(updatedRequestedById);
        oldAdditionalData.TryGetProperty(nameof(AdditionalDataObject.DisableOtp), out _).Should().BeFalse();
        oldAdditionalData.TryGetProperty(nameof(AdditionalDataObject.ChangeRoleOtp), out _).Should().BeFalse();
    }

    private static AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql("Host=localhost;Database=team-organization-test;Username=test;Password=test")
        .Options);
}
