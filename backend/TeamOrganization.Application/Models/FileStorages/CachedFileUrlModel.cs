namespace TeamOrganization.Application.Models.FileStorages;

public class CachedFileUrlModel
{
    public string Url { get; set; } = null!;
    public DateTimeOffset ExpiresAt { get; set; }
    public string ObjectKey { get; set; } = null!;
}