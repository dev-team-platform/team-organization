using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamOrganization.Api.Dtos.Common;
using TeamOrganization.Api.Dtos.V1.FileStorages;
using TeamOrganization.Application.Interfaces.Services.FileStorages;
using TeamOrganization.Application.Models.FileStorages;

namespace TeamOrganization.Api.Controllers.V1;

[Route("api/v{version:apiVersion}/file-storages")]
[ApiVersion("1.0")]
[ApiController]
[Authorize]
public class FileStorageController : ControllerBase
{
    private readonly IFileStorageQueryService _fileStorageQueryService;

    public FileStorageController(IFileStorageQueryService fileStorageQueryService)
    {
        _fileStorageQueryService = fileStorageQueryService;
    }

    [HttpPost("view")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetFileUrlResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetViewUrlAsync([FromBody] GetFileUrlRequest request, CancellationToken cancellationToken = default)
    {
        var model = GetFileUrlRequest.ToModel(request);
        var url = await _fileStorageQueryService.GetPresignedViewUrlAsync(model, cancellationToken);
        var result = new GetFileUrlResponseModel { Url = url };
        var dto = GetFileUrlResponse.FromModel(result);
        return Ok(dto);
    }

    [HttpPost("download")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetFileUrlResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetDownloadUrlAsync([FromBody] GetFileUrlRequest request, CancellationToken cancellationToken = default)
    {
        var model = GetFileUrlRequest.ToModel(request);
        var url = await _fileStorageQueryService.GetPresignedDownloadUrlAsync(model, cancellationToken);
        var result = new GetFileUrlResponseModel { Url = url };
        var dto = GetFileUrlResponse.FromModel(result);
        return Ok(dto);
    }
}
