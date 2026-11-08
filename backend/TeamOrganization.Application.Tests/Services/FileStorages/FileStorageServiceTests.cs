using AwesomeAssertions;
using Moq;
using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Application.Interfaces.Services.Cache;
using TeamOrganization.Application.Interfaces.Services.FileStorages;
using TeamOrganization.Application.Interfaces.Services.ObjectStorages;
using TeamOrganization.Application.Models.FileStorages;
using TeamOrganization.Application.Services.FileStorages;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Domain.Constants.CacheKeys;
using TeamOrganization.Domain.Exceptions;
using Xunit;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Domain.Constants.Uam;
using TeamOrganization.Domain.Enums.FileStorage;

namespace TeamOrganization.Application.Tests.Services.FileStorages;

public class FileStorageServiceTests
{
    private readonly Mock<IFileStorageRepository> _repository = new();
    private readonly Mock<IObjectStorageService> _objects = new();
    private readonly Mock<IUnitOfWork> _unit = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly Mock<IUserQueryService> _users = new();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetUrlAsync_ShouldRejectUserExport_WhenCurrentUserHasNoPermission(bool view)
    {
        var file = NewFile();
        file.FileType = FileType.UserListView;
        SetupFind(file);
        _users.Setup(x => x.HasPermissionsAsync(It.Is<IReadOnlyList<string>>(codes => codes.Count == 1 && codes[0] == PC.User.Export), false, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var action = () => view
            ? Query().GetPresignedViewUrlAsync(new GetFileUrlRequestModel { FileId = file.Id })
            : Query().GetPresignedDownloadUrlAsync(new GetFileUrlRequestModel { FileId = file.Id });

        await action.Should().ThrowAsync<ForbiddenException>();
        _cache.Verify(x => x.GetAsync<CachedFileUrlModel>(It.IsAny<CacheKeyObject>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPresignedDownloadUrlAsync_ShouldReturnCachedUrl_WhenUserExportIsAuthorized()
    {
        var file = NewFile();
        file.FileType = FileType.UserListView;
        SetupFind(file);
        _users.Setup(x => x.HasPermissionsAsync(It.Is<IReadOnlyList<string>>(codes => codes.Count == 1 && codes[0] == PC.User.Export), false, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _cache.Setup(x => x.GetAsync<CachedFileUrlModel>(It.IsAny<CacheKeyObject>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CachedFileUrlModel { Url = "cached-url", ObjectKey = file.ObjectKey, ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(3) });

        var result = await Query().GetPresignedDownloadUrlAsync(new GetFileUrlRequestModel { FileId = file.Id });

        result.Should().Be("cached-url");
        _users.Verify(x => x.HasPermissionsAsync(It.Is<IReadOnlyList<string>>(codes => codes.Count == 1 && codes[0] == PC.User.Export), false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateFileStorageAsync_ShouldUploadThenPersist_WhenSuccessful()
    {
        var order = new List<string>();
        var file = NewFile();
        file.Id = Guid.Empty;
        using var stream = new MemoryStream();
        _objects.Setup(x => x.UploadAsync(file.ObjectKey, stream, file.ContentType, It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("upload")).Returns(Task.CompletedTask);
        _repository.Setup(x => x.Add(file)).Callback(() => order.Add("add")).Returns(file);
        _unit.Setup(x => x.SaveChangesAsync(file.CreatedById!.Value.ToString(), It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("commit")).ReturnsAsync(1);

        var result = await Command().CreateFileStorageAsync(stream, file);

        result.Should().BeSameAs(file);
        file.Id.Version.Should().Be(7);
        order.Should().Equal("upload", "add", "commit");
        _objects.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateFileStorageAsync_ShouldDeleteUploadedObject_WhenCommitFailsOrIsCancelled()
    {
        var file = NewFile();
        using var stream = new MemoryStream();
        using var cancellation = new CancellationTokenSource();
        _objects.Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unit.Setup(x => x.SaveChangesAsync(It.IsAny<string>(), cancellation.Token))
            .Callback(() => cancellation.Cancel()).ThrowsAsync(new OperationCanceledException());
        _objects.Setup(x => x.DeleteAsync(file.ObjectKey, CancellationToken.None)).Returns(Task.CompletedTask);

        var action = () => Command().CreateFileStorageAsync(stream, file, cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
        _objects.Verify(x => x.DeleteAsync(file.ObjectKey, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CreateFileStorageAsync_ShouldNotCommitOrDelete_WhenUploadFails()
    {
        var file = NewFile();
        using var stream = new MemoryStream();
        _objects.Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("upload failed"));

        var action = () => Command().CreateFileStorageAsync(stream, file);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _repository.Verify(x => x.Add(It.IsAny<FileStorage>()), Times.Never);
        _objects.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPresignedDownloadUrlAsync_ShouldReturnCachedUrl_WhenFileIsValid()
    {
        var file = NewFile();
        SetupFind(file);
        var cacheKey = FileStorageCacheKeys.GetPresignedDownloadUrlKey(file.Id.ToString());
        _cache.Setup(x => x.GetAsync<CachedFileUrlModel>(It.Is<CacheKeyObject>(key => key.Key == cacheKey.Key), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CachedFileUrlModel { Url = "cached-url", ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(3), ObjectKey = file.ObjectKey });

        var url = await Query().GetPresignedDownloadUrlAsync(new GetFileUrlRequestModel { FileId = file.Id });

        url.Should().Be("cached-url");
        _objects.Verify(x => x.GetPresignedDownloadUrlAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
    }

    [Fact]
    public async Task GetPresignedViewUrlAsync_ShouldCacheForNinetyPercent_WhenFileHasExpiry()
    {
        var file = NewFile();
        file.ExpiredAt = DateTimeOffset.UtcNow.AddMinutes(5);
        SetupFind(file);
        DateTimeOffset signedExpiry = default;
        _objects.Setup(x => x.GetPresignedViewUrlAsync(file.ObjectKey, It.IsAny<DateTimeOffset>()))
            .Callback<string, DateTimeOffset>((_, expiry) => signedExpiry = expiry).ReturnsAsync("view-url");
        TimeSpan? ttl = null;
        _cache.Setup(x => x.SetAsync(It.IsAny<CacheKeyObject>(), It.IsAny<CachedFileUrlModel>(), It.IsAny<CancellationToken>()))
            .Callback<CacheKeyObject, CachedFileUrlModel, CancellationToken>((key, _, _) => ttl = key.Expiration)
            .Returns(Task.CompletedTask);
        var before = DateTimeOffset.UtcNow;

        var result = await Query().GetPresignedViewUrlAsync(new GetFileUrlRequestModel { FileId = file.Id });

        result.Should().Be("view-url");
        signedExpiry.Should().Be(file.ExpiredAt!.Value);
        ttl.Should().NotBeNull();
        ttl!.Value.Should().BeGreaterThan(TimeSpan.FromTicks((long)((signedExpiry - before).Ticks * 0.85)))
            .And.BeLessThan(TimeSpan.FromTicks((long)((signedExpiry - before).Ticks * 0.91)));
        _cache.Verify(x => x.SetAsync(It.Is<CacheKeyObject>(key => key.Key == FileStorageCacheKeys.GetPresignedViewUrlKey(file.Id.ToString()).Key), It.IsAny<CachedFileUrlModel>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPresignedDownloadUrlAsync_ShouldThrowUnprocessable_WhenFileExpiredEvenWithCache()
    {
        var file = NewFile();
        file.ExpiredAt = DateTimeOffset.UtcNow.AddSeconds(-1);
        SetupFind(file);
        var action = () => Query().GetPresignedDownloadUrlAsync(new GetFileUrlRequestModel { FileId = file.Id });

        await action.Should().ThrowAsync<UnprocessableEntityException>();
        _cache.Verify(x => x.GetAsync<CachedFileUrlModel>(It.IsAny<CacheKeyObject>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPresignedViewUrlAsync_ShouldThrowConflict_WhenReferenceMatchesMultipleFiles()
    {
        var reference = Guid.NewGuid();
        var older = NewFile();
        older.ReferenceId = reference.ToString();
        older.CreatedAt = DateTimeOffset.UtcNow.AddDays(-1);
        var newer = NewFile();
        newer.ReferenceId = reference.ToString();
        newer.CreatedAt = DateTimeOffset.UtcNow;
        SetupFind(older, newer);
        var action = () => Query().GetPresignedViewUrlAsync(new GetFileUrlRequestModel { ReferenceId = reference.ToString() });

        await action.Should().ThrowAsync<ConflictException>();
        _objects.Verify(x => x.GetPresignedViewUrlAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
    }

    [Fact]
    public async Task GetPresignedDownloadUrlAsync_ShouldThrowConflict_WhenIdentifiersMatchDifferentFiles()
    {
        var first = NewFile();
        var second = NewFile();
        second.ReferenceId = "different-reference";
        SetupFind(first, second);
        var request = new GetFileUrlRequestModel { FileId = first.Id, ReferenceId = second.ReferenceId };

        var action = () => Query().GetPresignedDownloadUrlAsync(request);

        await action.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task GetPresignedDownloadUrlAsync_ShouldFindRequestedFile_WhenOtherFilesHaveNullReferences()
    {
        var requested = NewFile();
        var other = NewFile();
        SetupFind(requested, other);
        _objects.Setup(x => x.GetPresignedDownloadUrlAsync(requested.ObjectKey, It.IsAny<DateTimeOffset>()))
            .ReturnsAsync("download-url");
        _cache.Setup(x => x.SetAsync(It.IsAny<CacheKeyObject>(), It.IsAny<CachedFileUrlModel>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await Query().GetPresignedDownloadUrlAsync(new GetFileUrlRequestModel { FileId = requested.Id });

        result.Should().Be("download-url");
    }

    [Fact]
    public async Task GetPresignedDownloadUrlAsync_ShouldRejectMissingIdentifier_WhenNoFileIdOrReferenceIdIsProvided()
    {
        var action = () => Query().GetPresignedDownloadUrlAsync(new GetFileUrlRequestModel());

        await action.Should().ThrowAsync<UnprocessableEntityException>();
        _repository.Verify(x => x.FindAllByConditionAsync(It.IsAny<Func<IQueryable<FileStorage>, IQueryable<FileStorage>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupFind(params FileStorage[] files) =>
        _repository.Setup(x => x.FindAllByConditionAsync(It.IsAny<Func<IQueryable<FileStorage>, IQueryable<FileStorage>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Func<IQueryable<FileStorage>, IQueryable<FileStorage>> predicate, bool _, CancellationToken _) => predicate(files.AsQueryable()).ToList());

    private FileStorageCommandService Command() => new(_repository.Object, _objects.Object, _unit.Object);
    private FileStorageQueryService Query() => new(_repository.Object, _objects.Object, _users.Object, _cache.Object);
    private static FileStorage NewFile() => new()
    {
        Id = Guid.CreateVersion7(),
        CreatedById = Guid.NewGuid(),
        ObjectKey = Guid.NewGuid().ToString(),
        ContentType = "text/plain",
        FileName = "file.txt",
        FileType = FileType.UserAvatar
    };
}
