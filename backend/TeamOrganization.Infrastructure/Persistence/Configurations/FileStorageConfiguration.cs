using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamOrganization.Domain.Entities;

namespace TeamOrganization.Infrastructure.Persistence.Configurations;

public class FileStorageConfiguration : IEntityTypeConfiguration<FileStorage>
{
    public void Configure(EntityTypeBuilder<FileStorage> builder)
    {
        builder.ToTable("file_storage");

        builder.Property(x => x.ReferenceId)
            .HasColumnName("reference_id")
            .HasMaxLength(255);

        builder.Property(x => x.ObjectKey)
            .HasColumnName("object_key")
            .HasMaxLength(4096)
            .IsRequired();

        builder.Property(x => x.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(x => x.FileType)
            .HasColumnName("file_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.FileSize)
            .HasColumnName("file_size")
            .IsRequired();

        builder.Property(x => x.ExpiredAt)
            .HasColumnName("expired_at");

        builder.HasIndex(x => x.ReferenceId)
            .HasDatabaseName("idx_file_storage_reference_id");

        builder.HasIndex(x => x.FileType)
            .HasDatabaseName("idx_file_storage_file_type");
    }
}
