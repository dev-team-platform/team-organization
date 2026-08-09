using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamOrganization.Domain.Entities;

namespace TeamOrganization.Infrastructure.Persistence.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.RoleId)
            .HasColumnName("role_id")
            .IsRequired();

        builder.Property(x => x.EffectiveFrom)
            .HasColumnName("effective_from")
            .IsRequired();

        builder.Property(x => x.EffectiveTo)
            .HasColumnName("effective_to")
            .IsRequired(false);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.UserId, x.RoleId })
            .IsUnique();
    }
}
