using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamOrganization.Domain.Entities;

namespace TeamOrganization.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasColumnName("description");

        builder.HasIndex(x => x.Code)
            .IsUnique();
    }
}
