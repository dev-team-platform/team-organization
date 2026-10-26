using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TeamOrganization.Domain.Exceptions;
using TeamOrganization.Infrastructure.Persistence.Extensions;
using Xunit;

namespace TeamOrganization.Infrastructure.Tests.Persistence;

public class DbUpdateConflictExtensionsTests
{
    [Fact]
    public void IsUniqueConstraintViolation_ShouldReturnTrue_WhenPostgresReportsUniqueViolation()
    {
        var postgresException = new PostgresException(
            "duplicate key", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation);
        var exception = new DbUpdateException("save failed", postgresException);

        exception.IsUniqueConstraintViolation().Should().BeTrue();
    }

    [Fact]
    public void IsUniqueConstraintViolation_ShouldReturnFalse_WhenInnerExceptionIsNotPostgresUniqueViolation()
    {
        var exception = new DbUpdateException("save failed", new InvalidOperationException("other error"));

        exception.IsUniqueConstraintViolation().Should().BeFalse();
    }

    [Fact]
    public void ToConflictException_ShouldReturnGenericConflict_WhenConstraintMetadataIsUnavailable()
    {
        var postgresException = new PostgresException(
            "duplicate key", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation);
        var exception = new DbUpdateException("save failed", postgresException);
        using var context = new DbContext(new DbContextOptionsBuilder().Options);

        var conflict = exception.ToConflictException(context);

        conflict.Should().BeOfType<ConflictException>();
        conflict.Message.Should().Be("Duplicate value for unique constraint.");
        conflict.Details.Should().BeNull();
    }
}
