using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Application.Interfaces.Services.Cache;
using TeamOrganization.Application.Interfaces.Services.FileStorages;
using TeamOrganization.Application.Interfaces.Services.ObjectStorages;
using TeamOrganization.Application.Models.FileStorages;
using TeamOrganization.Domain.Constants.CacheKeys;
using TeamOrganization.Domain.Exceptions;
using TeamOrganization.Domain.Enums.FileStorage;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Domain.Constants.Uam;

namespace TeamOrganization.Application.Services.FileStorages;

public class FileStorageQueryService : IFileStorageQueryService
{
    private readonly IFileStorageRepository _fileStorageRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IUserQueryService _userQueryService;
    private readonly ICacheService _cacheService;

    public FileStorageQueryService(
        IFileStorageRepository fileStorageRepository,
        IObjectStorageService objectStorageService,
        IUserQueryService userQueryService,
        ICacheService cacheService)
    {
        _fileStorageRepository = fileStorageRepository;
        _objectStorageService = objectStorageService;
        _userQueryService = userQueryService;
        _cacheService = cacheService;
    }

    public Task<string> GetPresignedDownloadUrlAsync(GetFileUrlRequestModel model, CancellationToken cancellationToken = default) =>
        GetUrlAsync(model, false, TimeSpan.FromMinutes(15), cancellationToken);

    public Task<string> GetPresignedViewUrlAsync(GetFileUrlRequestModel model, CancellationToken cancellationToken = default) =>
        GetUrlAsync(model, true, TimeSpan.FromHours(12), cancellationToken);

    private async Task ValidatePermission(FileType fileType, CancellationToken cancellationToken)
    {
        switch (fileType)
        {
            case FileType.UserAvatar:
                return;

            case FileType.UserListView:
                var hasPermission = await _userQueryService.HasPermissionsAsync(
                    [PC.User.Export],
                    cancellationToken: cancellationToken);

                if (!hasPermission)
                {
                    throw new ForbiddenException("You do not have permission to access this export.");
                }
                return;

            default:
                throw new UnprocessableEntityException("File type is not supported");
        }
    }

    private async Task<string> GetUrlAsync(
        GetFileUrlRequestModel model,
        bool isViewed,
        TimeSpan urlLifeTime,
        CancellationToken cancellationToken)
    {

        ArgumentNullException.ThrowIfNull(model);
        var fileId = model.FileId == Guid.Empty ? null : model.FileId;
        var referenceId = string.IsNullOrWhiteSpace(model.ReferenceId) ? null : model.ReferenceId;
        if (!fileId.HasValue && referenceId is null)
        {
            throw new UnprocessableEntityException("Provide a file ID or reference ID.");
        }

        var files = await _fileStorageRepository.FindAllByConditionAsync(
                query => query.Where(x => (fileId.HasValue && x.Id == fileId.Value)
                    || (referenceId != null && x.ReferenceId == referenceId))
                    .OrderBy(x => x.Id)
                    .Take(2),
                cancellationToken: cancellationToken);

        if (files.Count == 0)
        {
            throw new NotFoundException("File not found.");
        }
        if (files.Count > 1)
        {
            throw new ConflictException("Multiple files match the reference ID.");
        }
        var file = files[0];

        await ValidatePermission(file.FileType, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (file.ExpiredAt <= now)
        {
            throw new UnprocessableEntityException("The file has expired.");
        }

        var cacheKey = isViewed ?
            FileStorageCacheKeys.GetPresignedViewUrlKey(file.Id.ToString()) :
            FileStorageCacheKeys.GetPresignedDownloadUrlKey(file.Id.ToString());

        var cached = await _cacheService.GetAsync<CachedFileUrlModel>(cacheKey, cancellationToken);
        now = DateTimeOffset.UtcNow;
        if (file.ExpiredAt <= now)
        {
            throw new UnprocessableEntityException("The file has expired.");
        }
        if (cached is not null && cached.ObjectKey == file.ObjectKey && cached.ExpiresAt > now &&
            (!file.ExpiredAt.HasValue || cached.ExpiresAt <= file.ExpiredAt.Value))
        {
            return cached.Url;
        }

        var expiresAt = now.Add(urlLifeTime);
        if (file.ExpiredAt.HasValue && file.ExpiredAt.Value < expiresAt)
        {
            expiresAt = file.ExpiredAt.Value;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var url = isViewed
            ? await _objectStorageService.GetPresignedViewUrlAsync(file.ObjectKey, expiresAt)
            : await _objectStorageService.GetPresignedDownloadUrlAsync(file.ObjectKey, expiresAt);

        cancellationToken.ThrowIfCancellationRequested();

        var remaining = expiresAt - DateTimeOffset.UtcNow;

        if (remaining <= TimeSpan.Zero)
        {
            throw new UnprocessableEntityException("The file has expired.");
        }

        var cacheLifetime = TimeSpan.FromTicks((long)(remaining.Ticks * (90.0 / 100.0)));
        if (cacheLifetime > TimeSpan.FromMilliseconds(1))
        {
            cacheKey.Expiration = cacheLifetime;
            await _cacheService.SetAsync(
                cacheKey,
                new CachedFileUrlModel
                {
                    Url = url,
                    ObjectKey = file.ObjectKey,
                    ExpiresAt = now.Add(cacheLifetime)
                },
                cancellationToken);
        }

        return url;
    }
}
