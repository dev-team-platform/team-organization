using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamOrganization.Api.Dtos.Common;
using TeamOrganization.Api.Dtos.V1.Users;
using TeamOrganization.Application.Interfaces.Services.Users;

namespace TeamOrganization.Api.Controllers.V1;

[Route("api/v{version:apiVersion}/users")]
[ApiVersion("1.0")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly Serilog.ILogger _logger;
    private readonly IUserQueryService _userQueryService;
    private readonly IUserCommandService _userCommandService;

    public UserController(
        Serilog.ILogger logger,
        IUserQueryService userQueryService,
        IUserCommandService userCommandService)
    {
        _logger = logger;
        _userQueryService = userQueryService;
        _userCommandService = userCommandService;
    }

    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetCurrentUserResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var result = await _userQueryService.GetCurrentUserAsync(cancellationToken);
        var dtoResponse = GetCurrentUserResponse.FromModel(result);
        return Ok(dtoResponse);
    }

}