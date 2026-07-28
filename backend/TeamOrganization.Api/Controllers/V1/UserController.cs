using Microsoft.AspNetCore.Mvc;
using TeamOrganization.Application.Interfaces.Services;

namespace TeamOrganization.Api.Controllers.V1;

[Route("api/v{version:apiVersion}/users")]
[ApiVersion("1.0")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserQueryService _userQueryService;
    private readonly IUserCommandService _userCommandService;

    public UserController(IUserQueryService userQueryService, IUserCommandService userCommandService)
    {
        _userQueryService = userQueryService;
        _userCommandService = userCommandService;
    }
}