namespace TeamOrganization.Application.Interfaces.Services.ObjectStorages;

public interface IObjectStorageService
{
    Task UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task DownloadAsync(string key, Stream destination, CancellationToken cancellationToken = default);

    Task<string> GetPresignedDownloadUrlAsync(string key, DateTimeOffset expiresAt);

    Task<string> GetPresignedViewUrlAsync(string key, DateTimeOffset expiresAt);

    Task<string> GetPresignedUploadUrlAsync(string key, DateTimeOffset expiresAt);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
