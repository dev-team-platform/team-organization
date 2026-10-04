using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using TeamOrganization.Api.Constants;
using TeamOrganization.Api.Attributes;
using TeamOrganization.Api.Dtos.Common;
using TeamOrganization.Api.Dtos.V1.Users;
using TeamOrganization.Api.Options;
using TeamOrganization.Api.Services;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Constants;
using TeamOrganization.Domain.Exceptions;

namespace TeamOrganization.Api.Controllers.V1;

[Route("api/v{version:apiVersion}/users")]
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[EnableRateLimiting(RateLimiterPolicies.Default)]
public class UserController : ControllerBase
{
    private readonly Serilog.ILogger _logger;
    private readonly IUserQueryService _userQueryService;
    private readonly IUserCommandService _userCommandService;
    private readonly IKeycloakService _keycloakService;
    private readonly IOptions<UserManagementOptions> _userManagementOptions;

    public UserController(
        Serilog.ILogger logger,
        IUserQueryService userQueryService,
        IUserCommandService userCommandService,
        IKeycloakService keycloakService,
        IOptions<UserManagementOptions> userManagementOptions)
    {
        _logger = logger;
        _userQueryService = userQueryService;
        _userCommandService = userCommandService;
        _keycloakService = keycloakService;
        _userManagementOptions = userManagementOptions;
    }

    [HttpGet("me")]
    [Permissions(PermissionCodes.UserSelf.Read)]
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

    [HttpPost("new-user")]
    [Permissions(PermissionCodes.User.Create)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CreateNewUserResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> CreateNewUserAsync(
        [FromBody] CreateNewUserRequest request,
        CancellationToken cancellationToken = default)
    {
        return await CreateNewUserInternalAsync(
            request,
            [RoleCodes.Employee],
            cancellationToken);
    }

    [HttpPost("new-admin-user")]
    [Permissions(PermissionCodes.Admin.Create)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CreateNewUserResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> CreateNewAdminUserAsync(
        [FromBody] CreateNewUserRequest request,
        CancellationToken cancellationToken = default)
    {
        return await CreateNewUserInternalAsync(
            request,
            [RoleCodes.Admin],
            cancellationToken);
    }

    private async Task<IActionResult> CreateNewUserInternalAsync(
        CreateNewUserRequest request,
        List<string> expectedRoleCodes,
        CancellationToken cancellationToken)
    {
        if (!expectedRoleCodes.Contains(request.RoleCode))
        {
            throw new UnprocessableEntityException("Invalid role code", new Dictionary<string, object?>
            {
                ["roleCode"] = request.RoleCode
            });
        }

        var accessToken = await _keycloakService.GetAccessTokenAsync(cancellationToken);
        var identitySubject = await _keycloakService.CreateNewUserAsync(
            accessToken,
            new KeycloakNewUser
            {
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName
            },
            _userManagementOptions.Value.DefaultPassword,
            cancellationToken);

        var result = await _userCommandService.CreateNewUserAsync(
            new CreateUserRequestModel
            {
                IdentitySubject = identitySubject,
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DisplayName = request.DisplayName,
                EmployeeCode = request.EmployeeCode
            },
            cancellationToken);

        var dtoResponse = CreateNewUserResponse.FromModel(result);

        return StatusCode(StatusCodes.Status201Created, dtoResponse);
    }

}
