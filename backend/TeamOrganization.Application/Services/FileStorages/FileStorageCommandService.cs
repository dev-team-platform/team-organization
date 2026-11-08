using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Application.Interfaces.Services.FileStorages;
using TeamOrganization.Application.Interfaces.Services.ObjectStorages;
using TeamOrganization.Domain.Entities;

namespace TeamOrganization.Application.Services.FileStorages;

public class FileStorageCommandService : IFileStorageCommandService
{
    private readonly IFileStorageRepository _fileStorageRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IUnitOfWork _unitOfWork;

    public FileStorageCommandService(
        IFileStorageRepository fileStorageRepository,
        IObjectStorageService objectStorageService,
        IUnitOfWork unitOfWork)
    {
        _fileStorageRepository = fileStorageRepository;
        _objectStorageService = objectStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<FileStorage> CreateFileStorageAsync(Stream content, FileStorage fileStorage, CancellationToken cancellationToken = default)
    {
        if (fileStorage.CreatedById is null || fileStorage.CreatedById == Guid.Empty)
        {
            throw new ArgumentException("The creator ID is required.", nameof(fileStorage));
        }

        if (fileStorage.Id == Guid.Empty)
        {
            fileStorage.Id = Guid.CreateVersion7();
        }

        await _objectStorageService.UploadAsync(fileStorage.ObjectKey, content, fileStorage.ContentType, cancellationToken);

        try
        {
            _fileStorageRepository.Add(fileStorage);
            await _unitOfWork.SaveChangesAsync(fileStorage.CreatedById.Value.ToString(), cancellationToken);
        }
        catch (Exception ex)
        {
            try
            {
                await _objectStorageService.DeleteAsync(fileStorage.ObjectKey, CancellationToken.None);
            }
            catch (Exception cleanupEx)
            {
                throw new AggregateException("Saving file metadata and deleting the uploaded object both failed.", ex, cleanupEx);
            }
            throw;
        }

        return fileStorage;
    }
}
