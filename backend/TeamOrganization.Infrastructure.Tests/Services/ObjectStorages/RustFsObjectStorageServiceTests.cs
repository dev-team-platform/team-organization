using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using TeamOrganization.Application.Interfaces.Services.ObjectStorages;
using TeamOrganization.Infrastructure.Options;
using TeamOrganization.Infrastructure.Services.ObjectStorages;
using Xunit;

namespace TeamOrganization.Infrastructure.Tests.Services.ObjectStorages;

public class RustFsObjectStorageServiceTests
{
    [Theory]
    [InlineData("http://localhost:9000", Protocol.HTTP)]
    [InlineData("https://storage.example", Protocol.HTTPS)]
    public async Task GetPresignedUrlAsync_ShouldUseConfiguredEndpointProtocol_WhenSigning(string endpoint, Protocol protocol)
    {
        var client = new Mock<IAmazonS3>();
        GetPreSignedUrlRequest? sent = null;
        client.Setup(x => x.GetPreSignedURLAsync(It.IsAny<GetPreSignedUrlRequest>()))
            .Callback<GetPreSignedUrlRequest>(request => sent = request)
            .ReturnsAsync("signed-url");
        var service = CreateService(client.Object, endpoint);

        await service.GetPresignedDownloadUrlAsync("file.xlsx", DateTimeOffset.UtcNow.AddMinutes(5));

        sent.Should().NotBeNull();
        sent!.Protocol.Should().Be(protocol);
    }

    [Theory]
    [InlineData("http://localhost:9000", "http")]
    [InlineData("https://storage.example", "https")]
    public async Task GetPresignedUrlAsync_ShouldGenerateUrlsWithEndpointScheme_WhenUsingS3Client(string endpoint, string scheme)
    {
        using var client = new AmazonS3Client(new BasicAWSCredentials("test-key", "test-secret"), new AmazonS3Config
        {
            ServiceURL = endpoint,
            AuthenticationRegion = "us-east-1",
            ForcePathStyle = true
        });
        var service = CreateService(client, endpoint);
        var expiry = DateTimeOffset.UtcNow.AddMinutes(5);

        var download = await service.GetPresignedDownloadUrlAsync("file.xlsx", expiry);
        var view = await service.GetPresignedViewUrlAsync("file.xlsx", expiry);
        var upload = await service.GetPresignedUploadUrlAsync("file.xlsx", expiry);

        new Uri(download).Scheme.Should().Be(scheme);
        new Uri(view).Scheme.Should().Be(scheme);
        new Uri(upload).Scheme.Should().Be(scheme);
    }

    [Fact]
    public async Task UploadAsync_ShouldSendObjectWithoutClosingOrRewindingStream_WhenCalled()
    {
        var client = new Mock<IAmazonS3>();
        using var content = new MemoryStream([1, 2, 3]);
        content.Position = 1;
        using var cancellation = new CancellationTokenSource();
        PutObjectRequest? sent = null;
        client.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), cancellation.Token))
            .Callback<PutObjectRequest, CancellationToken>((request, _) => sent = request)
            .ReturnsAsync(new PutObjectResponse());
        var service = CreateService(client.Object);

        await service.UploadAsync("folder/file.txt", content, "text/plain", cancellation.Token);

        sent.Should().NotBeNull();
        sent!.BucketName.Should().Be("files");
        sent.Key.Should().Be("folder/file.txt");
        sent.ContentType.Should().Be("text/plain");
        sent.InputStream.Should().BeSameAs(content);
        sent.AutoCloseStream.Should().BeFalse();
        sent.AutoResetStreamPosition.Should().BeFalse();
        content.Position.Should().Be(1);
        content.CanRead.Should().BeTrue();
    }

    [Fact]
    public async Task DownloadAsync_ShouldCopyAndDisposeResponse_WhenObjectIsFound()
    {
        var client = new Mock<IAmazonS3>();
        var responseStream = new MemoryStream(Encoding.UTF8.GetBytes("file contents"));
        var response = new GetObjectResponse { ResponseStream = responseStream };
        using var cancellation = new CancellationTokenSource();
        client
            .Setup(x => x.GetObjectAsync(
                It.Is<GetObjectRequest>(r => r.BucketName == "files" && r.Key == "file.txt"),
                cancellation.Token))
            .ReturnsAsync(response);
        using var destination = new MemoryStream();
        var service = CreateService(client.Object);

        await service.DownloadAsync("file.txt", destination, cancellation.Token);

        Encoding.UTF8.GetString(destination.ToArray()).Should().Be("file contents");
        responseStream.CanRead.Should().BeFalse();
        destination.CanWrite.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteConfiguredObject_WhenCalled()
    {
        var client = new Mock<IAmazonS3>();
        using var cancellation = new CancellationTokenSource();
        client.Setup(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), cancellation.Token))
            .ReturnsAsync(new DeleteObjectResponse());
        var service = CreateService(client.Object);

        await service.DeleteAsync("file.txt", cancellation.Token);

        client.Verify(x => x.DeleteObjectAsync(It.Is<DeleteObjectRequest>(r => r.BucketName == "files" && r.Key == "file.txt"), cancellation.Token), Times.Once);
    }

    [Theory]
    [InlineData(false, HttpVerb.GET)]
    [InlineData(true, HttpVerb.PUT)]
    public async Task GetPresignedUrlAsync_ShouldSignConfiguredObject_WhenExpiryIsValid(bool upload, HttpVerb verb)
    {
        var client = new Mock<IAmazonS3>();
        GetPreSignedUrlRequest? sent = null;
        client.Setup(x => x.GetPreSignedURLAsync(It.IsAny<GetPreSignedUrlRequest>()))
            .Callback<GetPreSignedUrlRequest>(request => sent = request)
            .ReturnsAsync("https://storage.example/signed");
        var service = CreateService(client.Object);
        var expiry = DateTimeOffset.UtcNow.AddMinutes(10).ToOffset(TimeSpan.FromHours(7));

        var result = upload
            ? await service.GetPresignedUploadUrlAsync("folder/file.txt", expiry)
            : await service.GetPresignedDownloadUrlAsync("folder/file.txt", expiry);

        result.Should().Be("https://storage.example/signed");
        sent.Should().NotBeNull();
        sent!.BucketName.Should().Be("files");
        sent.Key.Should().Be("folder/file.txt");
        sent.Verb.Should().Be(verb);
        sent.Expires.Should().Be(expiry.UtcDateTime);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(8)]
    public async Task GetPresignedUrlAsync_ShouldRejectExpiry_WhenOutsideAllowedRange(int days)
    {
        var client = new Mock<IAmazonS3>();
        var service = CreateService(client.Object);
        var expiry = DateTimeOffset.UtcNow.AddDays(days);

        var download = () => service.GetPresignedDownloadUrlAsync("file.txt", expiry);
        var upload = () => service.GetPresignedUploadUrlAsync("file.txt", expiry);

        await download.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await upload.Should().ThrowAsync<ArgumentOutOfRangeException>();
        client.Verify(x => x.GetPreSignedURLAsync(It.IsAny<GetPreSignedUrlRequest>()), Times.Never);
    }

    [Theory]
    [InlineData(false, "attachment")]
    [InlineData(true, "inline")]
    public async Task GetPresignedUrlAsync_ShouldSetResponseDisposition_WhenUrlIsGenerated(bool view, string disposition)
    {
        var client = new Mock<IAmazonS3>();
        GetPreSignedUrlRequest? sent = null;
        client.Setup(x => x.GetPreSignedURLAsync(It.IsAny<GetPreSignedUrlRequest>()))
            .Callback<GetPreSignedUrlRequest>(request => sent = request)
            .ReturnsAsync("https://storage.example/signed");
        var service = CreateService(client.Object);

        if (view)
            await service.GetPresignedViewUrlAsync("file.txt", DateTimeOffset.UtcNow.AddMinutes(5));
        else
            await service.GetPresignedDownloadUrlAsync("file.txt", DateTimeOffset.UtcNow.AddMinutes(5));

        sent!.ResponseHeaderOverrides.ContentDisposition.Should().Be(disposition);
    }

    [Fact]
    public void AddInfrastructure_ShouldConfigureS3Client_WhenObjectStorageOptionsAreProvided()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test",
            ["Redis:ConnectionString"] = "localhost:6379",
            ["ObjectStorage:Endpoint"] = "http://localhost:9002",
            ["ObjectStorage:BucketName"] = "files",
            ["ObjectStorage:Region"] = "us-east-1",
            ["ObjectStorage:AccessKey"] = "test-access-key",
            ["ObjectStorage:SecretKey"] = "test-secret-key",
            ["ObjectStorage:ForcePathStyle"] = "true"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(config);
        services.AddAppOptions(config);
        services.AddInfrastructure(config);
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IAmazonS3>();
        var s3Config = (AmazonS3Config)client.Config;
        new Uri(s3Config.ServiceURL).Should().Be(new Uri("http://localhost:9002"));
        s3Config.AuthenticationRegion.Should().Be("us-east-1");
        s3Config.ForcePathStyle.Should().BeTrue();
        provider.GetRequiredService<IObjectStorageService>().Should().BeOfType<RustFsObjectStorageService>();
    }

    private static RustFsObjectStorageService CreateService(IAmazonS3 client, string endpoint = "https://storage.example") =>
        new(client, Microsoft.Extensions.Options.Options.Create(new ObjectStorageOptions { BucketName = "files", Endpoint = endpoint }));
}
