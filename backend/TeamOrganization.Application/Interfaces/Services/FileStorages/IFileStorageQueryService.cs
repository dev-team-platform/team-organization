using TeamOrganization.Application.Models.FileStorages;

namespace TeamOrganization.Application.Interfaces.Services.FileStorages;

public interface IFileStorageQueryService
{
    Task<string> GetPresignedDownloadUrlAsync(GetFileUrlRequestModel model, CancellationToken cancellationToken = default);
    Task<string> GetPresignedViewUrlAsync(GetFileUrlRequestModel model, CancellationToken cancellationToken = default);
}
