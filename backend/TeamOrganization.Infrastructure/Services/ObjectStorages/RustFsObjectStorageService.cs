using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using TeamOrganization.Application.Interfaces.Services.ObjectStorages;
using TeamOrganization.Infrastructure.Options;

namespace TeamOrganization.Infrastructure.Services.ObjectStorages;

public class RustFsObjectStorageService : IObjectStorageService
{
    private readonly IAmazonS3 _client;
    private readonly string _bucketName;
    private readonly Protocol _presignedUrlProtocol;

    public RustFsObjectStorageService(IAmazonS3 client, IOptions<ObjectStorageOptions> options)
    {
        _client = client;
        _bucketName = options.Value.BucketName;
        _presignedUrlProtocol = new Uri(options.Value.Endpoint).Scheme == Uri.UriSchemeHttp
            ? Protocol.HTTP
            : Protocol.HTTPS;
    }

    public async Task UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            AutoResetStreamPosition = false
        }, cancellationToken);
    }

    public async Task DownloadAsync(string key, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(destination);

        using var response = await _client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        }, cancellationToken);

        await response.ResponseStream.CopyToAsync(destination, cancellationToken);
    }

    public Task<string> GetPresignedDownloadUrlAsync(string key, DateTimeOffset expiresAt) =>
        GetPresignedUrlAsync(key, expiresAt, HttpVerb.GET, "attachment");

    public Task<string> GetPresignedViewUrlAsync(string key, DateTimeOffset expiresAt) =>
        GetPresignedUrlAsync(key, expiresAt, HttpVerb.GET, "inline");

    public Task<string> GetPresignedUploadUrlAsync(string key, DateTimeOffset expiresAt) =>
        GetPresignedUrlAsync(key, expiresAt, HttpVerb.PUT);

    private Task<string> GetPresignedUrlAsync(string key, DateTimeOffset expiresAt, HttpVerb verb, string? contentDisposition = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var lifetime = expiresAt - DateTimeOffset.UtcNow;
        if (lifetime <= TimeSpan.Zero || lifetime > TimeSpan.FromDays(7))
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAt), "Expiry must be in the future and no more than 7 days away.");
        }

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Expires = expiresAt.UtcDateTime,
            Verb = verb,
            Protocol = _presignedUrlProtocol
        };
        if (contentDisposition is not null)
            request.ResponseHeaderOverrides.ContentDisposition = contentDisposition;

        return _client.GetPreSignedURLAsync(request);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        await _client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        }, cancellationToken);
    }
}
