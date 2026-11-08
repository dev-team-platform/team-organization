using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamOrganization.Domain.Entities;

namespace TeamOrganization.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Description)
            .HasColumnName("description");

        builder.HasIndex(x => x.Code)
            .IsUnique();
    }
}
