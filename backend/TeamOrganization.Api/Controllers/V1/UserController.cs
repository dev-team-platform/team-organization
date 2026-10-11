using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using TeamOrganization.Api.Constants;
using TeamOrganization.Api.Attributes;
using TeamOrganization.Api.Dtos.Common;
using TeamOrganization.Api.Dtos.V1.Users;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Exceptions;
using TeamOrganization.Application.Interfaces.Services.IdentityProviders;
using TeamOrganization.Infrastructure.Options;
using TeamOrganization.Domain.Constants.Uam;
using TeamOrganization.Application.Models.IdentityProviders;

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
    private readonly IIdentityProviderService _identityProviderService;
    private readonly IOptions<UserManagementOptions> _userManagementOptions;

    public UserController(
        Serilog.ILogger logger,
        IUserQueryService userQueryService,
        IUserCommandService userCommandService,
        IIdentityProviderService identityProviderService,
        IOptions<UserManagementOptions> userManagementOptions)
    {
        _logger = logger;
        _userQueryService = userQueryService;
        _userCommandService = userCommandService;
        _identityProviderService = identityProviderService;
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

    [HttpPost("all")]
    [Permissions(PermissionCodes.User.Read)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetAllUsersResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetAllUsersAsync(
        [FromBody] GetAllUsersRequest request,
        CancellationToken cancellationToken = default)
    {
        var model = GetAllUsersRequest.ToModel(request);
        var result = await _userQueryService.GetAllUsersAsync(model, cancellationToken);
        var dtoResponse = GetAllUsersResponse.FromModel(result);
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
        [FromBody] CreateUserRequest request,
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
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        return await CreateNewUserInternalAsync(
            request,
            [RoleCodes.Admin],
            cancellationToken);
    }

    private async Task<IActionResult> CreateNewUserInternalAsync(
        CreateUserRequest request,
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

        var accessToken = await _identityProviderService.GetAccessTokenAsync(cancellationToken);
        var identitySubject = await _identityProviderService.CreateNewUserAsync(
            accessToken,
            new IdentityProviderNewUserRequestModel
            {
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName
            },
            _userManagementOptions.Value.DefaultPassword,
            cancellationToken);

        CreateUserResponseModel result;
        try
        {
            var model = CreateUserRequest.ToModel(request);
            model.IdentitySubject = identitySubject;
            result = await _userCommandService.CreateNewUserAsync(model, cancellationToken);
        }
        catch
        {
            await TryDeleteKeycloakUserAfterFailedCreateAsync(identitySubject);
            throw;
        }

        var dtoResponse = CreateNewUserResponse.FromModel(result);

        return StatusCode(StatusCodes.Status201Created, dtoResponse);
    }

    private async Task TryDeleteKeycloakUserAfterFailedCreateAsync(string identitySubject)
    {
        using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        try
        {
            var accessToken = await _identityProviderService.GetAccessTokenAsync(cleanupTimeout.Token);
            await _identityProviderService.DeleteUserAsync(
                accessToken,
                identitySubject,
                cleanupTimeout.Token);
        }
        catch (Exception cleanupException)
        {
            _logger.Error(
                cleanupException,
                "Failed to delete Keycloak user {IdentitySubject} after application user creation failed",
                identitySubject);
        }
    }

}
