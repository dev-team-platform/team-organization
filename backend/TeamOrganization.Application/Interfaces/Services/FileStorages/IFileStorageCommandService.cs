using TeamOrganization.Domain.Entities;

namespace TeamOrganization.Application.Interfaces.Services.FileStorages;

public interface IFileStorageCommandService
{
    Task<FileStorage> CreateFileStorageAsync(Stream content, FileStorage fileStorage, CancellationToken cancellationToken = default);
}
