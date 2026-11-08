using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamOrganization.Api.Attributes;
using TeamOrganization.Api.Dtos.Common;
using TeamOrganization.Api.Dtos.V1.Exports;
using TeamOrganization.Api.Dtos.V1.Users;
using TeamOrganization.Application.Interfaces.Services.Exports;
using TeamOrganization.Domain.Constants.Uam;

namespace TeamOrganization.Api.Controllers.V1;

[Route("api/v{version:apiVersion}/export")]
[ApiVersion("1.0")]
[ApiController]
[Authorize]
public sealed class ExportController : ControllerBase
{
    private readonly IExportService _exportService;
    public ExportController(IExportService exportService)
    {
        _exportService = exportService;
    }

    [HttpPost("users")]
    [Permissions(PC.User.Export)]
    [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(ExportResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> ExportUsersAsync([FromBody] GetAllUsersRequest request, CancellationToken cancellationToken = default)
    {
        var model = GetAllUsersRequest.ToModel(request);
        var result = await _exportService.ExportUsersAsync(model, cancellationToken);
        var dto = ExportResponse.FromModel(result);
        return Accepted(dto);
    }
}
