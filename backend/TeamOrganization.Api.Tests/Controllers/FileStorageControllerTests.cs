using System.ComponentModel.DataAnnotations;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TeamOrganization.Api.Controllers.V1;
using TeamOrganization.Api.Dtos.V1.FileStorages;
using TeamOrganization.Application.Interfaces.Services.FileStorages;
using TeamOrganization.Application.Models.FileStorages;
using TeamOrganization.Domain.Exceptions;
using Xunit;

namespace TeamOrganization.Api.Tests.Controllers;

public class FileStorageControllerTests
{
    private readonly Mock<IFileStorageQueryService> _query = new();

    [Fact]
    public async Task GetViewUrlAsync_ShouldMapRequestAndReturnUrl_WhenQuerySucceeds()
    {
        using var cancellation = new CancellationTokenSource();
        var request = new GetFileUrlRequest { FileId = Guid.NewGuid(), ReferenceId = "reference" };
        _query.Setup(x => x.GetPresignedViewUrlAsync(It.IsAny<GetFileUrlRequestModel>(), cancellation.Token))
            .ReturnsAsync("https://storage.example/view");
        var controller = new FileStorageController(_query.Object);

        var response = (await controller.GetViewUrlAsync(request, cancellation.Token)).Should().BeOfType<OkObjectResult>().Which;

        response.Value.Should().BeOfType<GetFileUrlResponse>().Which.Url.Should().Be("https://storage.example/view");
        _query.Verify(x => x.GetPresignedViewUrlAsync(
            It.Is<GetFileUrlRequestModel>(model => model.FileId == request.FileId && model.ReferenceId == request.ReferenceId),
            cancellation.Token), Times.Once);
        _query.Verify(x => x.GetPresignedDownloadUrlAsync(It.IsAny<GetFileUrlRequestModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetDownloadUrlAsync_ShouldMapRequestAndReturnUrl_WhenQuerySucceeds()
    {
        using var cancellation = new CancellationTokenSource();
        var request = new GetFileUrlRequest { ReferenceId = "reference" };
        _query.Setup(x => x.GetPresignedDownloadUrlAsync(It.IsAny<GetFileUrlRequestModel>(), cancellation.Token))
            .ReturnsAsync("https://storage.example/download");
        var controller = new FileStorageController(_query.Object);

        var response = (await controller.GetDownloadUrlAsync(request, cancellation.Token)).Should().BeOfType<OkObjectResult>().Which;

        response.Value.Should().BeOfType<GetFileUrlResponse>().Which.Url.Should().Be("https://storage.example/download");
        _query.Verify(x => x.GetPresignedDownloadUrlAsync(
            It.Is<GetFileUrlRequestModel>(model => model.FileId == null && model.ReferenceId == "reference"),
            cancellation.Token), Times.Once);
        _query.Verify(x => x.GetPresignedViewUrlAsync(It.IsAny<GetFileUrlRequestModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetViewUrlAsync_ShouldPropagateException_WhenFileHasExpired()
    {
        _query.Setup(x => x.GetPresignedViewUrlAsync(It.IsAny<GetFileUrlRequestModel>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnprocessableEntityException("The file has expired."));
        var controller = new FileStorageController(_query.Object);

        var action = () => controller.GetViewUrlAsync(new GetFileUrlRequest { FileId = Guid.NewGuid() });

        await action.Should().ThrowAsync<UnprocessableEntityException>();
    }

    [Theory]
    [InlineData(null, null, false)]
    [InlineData(null, "  ", false)]
    [InlineData("00000000-0000-0000-0000-000000000000", null, false)]
    [InlineData("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", null, true)]
    [InlineData(null, "reference", true)]
    public void Validate_ShouldRequireAnIdentifier_WhenRequestIsValidated(string? fileId, string? referenceId, bool valid)
    {
        var request = new GetFileUrlRequest { FileId = fileId is null ? null : Guid.Parse(fileId), ReferenceId = referenceId };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

        isValid.Should().Be(valid);
    }

    [Fact]
    public void Controller_ShouldRequireAuthentication_WhenEndpointsAreAccessed()
    {
        typeof(FileStorageController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Should().NotBeEmpty();
    }
}
