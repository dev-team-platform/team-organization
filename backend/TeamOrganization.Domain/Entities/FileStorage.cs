using TeamOrganization.Domain.Enums.FileStorage;

namespace TeamOrganization.Domain.Entities;

public class FileStorage : EntityBase
{
    public string? ReferenceId { get; set; }
    public string ObjectKey { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public FileType FileType { get; set; }
    public string ContentType { get; set; } = null!;
    public long FileSize { get; set; }
    public DateTimeOffset? ExpiredAt { get; set; }
}
