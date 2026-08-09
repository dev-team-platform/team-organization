using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamOrganization.Domain.Entities;

namespace TeamOrganization.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.IdentitySubject)
            .HasColumnName("identity_subject")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.EmployeeCode)
            .HasColumnName("employee_code")
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Username)
            .HasColumnName("username")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.FirstName)
            .HasColumnName("first_name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100);

        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.AvatarUrl)
            .HasColumnName("avatar_url");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.LastLoginAt)
            .HasColumnName("last_login_at");

        builder.Property(x => x.LastLogoutAt)
            .HasColumnName("last_logout_at");

        builder.HasIndex(x => x.IdentitySubject)
            .IsUnique();

        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.HasIndex(x => x.Username)
            .IsUnique();

        builder.HasIndex(x => x.EmployeeCode)
            .IsUnique();

        builder.HasIndex(x => x.DisplayName);
    }
}