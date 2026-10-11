using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamOrganization.Domain.Entities;

namespace TeamOrganization.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.Action)
            .HasColumnName("action")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.TraceId).HasColumnName("trace_id").HasMaxLength(64);
        builder.Property(x => x.SpanId).HasColumnName("span_id").HasMaxLength(32);
        builder.Property(x => x.RequestId).HasColumnName("request_id").HasMaxLength(255);
        builder.Property(x => x.ClientActionId).HasColumnName("client_action_id").HasMaxLength(255);
        builder.Property(x => x.ClientRequestId).HasColumnName("client_request_id").HasMaxLength(255);

        builder.Property(x => x.EntityName)
            .HasColumnName("entity_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.EntityId)
            .HasColumnName("entity_id")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.OldValues).HasColumnName("old_values").HasColumnType("jsonb");
        builder.Property(x => x.NewValues).HasColumnName("new_values").HasColumnType("jsonb");

        builder.Property(x => x.ActorId)
            .HasColumnName("actor_id")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.EntityName, x.EntityId });
        builder.HasIndex(x => x.ActorId);
    }
}
