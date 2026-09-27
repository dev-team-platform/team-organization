using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Domain.Entities;

namespace TeamOrganization.Infrastructure.Persistence;

public class AppDbContext : DbContext, IUnitOfWork
{
    internal Guid? CurrentActorId { get; private set; }

    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    #region DbSets
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    #endregion

    public async Task<int> SaveChangesAsync(
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        CurrentActorId = actorId;

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            CurrentActorId = null;
        }
    }

    public async Task<int> SaveChangesWithoutAuditAsync(CancellationToken cancellationToken = default)
    {
        CurrentActorId = null;
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureBaseEntities(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    private static void ConfigureBaseEntities(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(EntityBase).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var builder = modelBuilder.Entity(entityType.ClrType);

            builder.HasKey(nameof(EntityBase.Id));

            builder.Property(nameof(EntityBase.Id))
                .HasColumnName("id")
                .ValueGeneratedNever()
                .IsRequired();

            builder.Property(nameof(EntityBase.CreatedAt))
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(nameof(EntityBase.CreatedById))
                .HasColumnName("created_by_id")
                .IsRequired(false);

            builder.HasOne(nameof(EntityBase.CreatedBy))
                .WithMany()
                .HasForeignKey(nameof(EntityBase.CreatedById))
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(nameof(EntityBase.UpdatedAt))
                .HasColumnName("updated_at")
                .IsRequired(false);

            builder.Property(nameof(EntityBase.UpdatedById))
                .HasColumnName("updated_by_id")
                .IsRequired(false);

            builder.HasOne(nameof(EntityBase.UpdatedBy))
                .WithMany()
                .HasForeignKey(nameof(EntityBase.UpdatedById))
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(nameof(EntityBase.IsDeleted))
                .HasColumnName("is_deleted");

            builder.Property(nameof(EntityBase.DeletedAt))
                .HasColumnName("deleted_at")
                .IsRequired(false);

            ApplySoftDeleteFilter(modelBuilder, entityType.ClrType);
        }
    }

    private static void ApplySoftDeleteFilter(ModelBuilder modelBuilder, Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "entity");

        var isDeletedProperty = Expression.Property(
            parameter,
            nameof(EntityBase.IsDeleted));

        var condition = Expression.NotEqual(
            isDeletedProperty,
            Expression.Constant(true, typeof(bool)));

        var lambda = Expression.Lambda(condition, parameter);

        modelBuilder.Entity(entityType)
            .HasQueryFilter(lambda);
    }
}
