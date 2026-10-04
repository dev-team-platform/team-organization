using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Domain.Exceptions;

namespace TeamOrganization.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class PermissionsAttribute : TypeFilterAttribute
{
    public PermissionsAttribute(params string[] permissionCodes)
        : base(typeof(PermissionsAuthorizationFilter))
    {
        Arguments = [permissionCodes];
    }
}

public sealed class PermissionsAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly IUserQueryService _userQueryService;
    private readonly IReadOnlyCollection<string> _permissionCodes;

    public PermissionsAuthorizationFilter(
        IUserQueryService userQueryService,
        string[] permissionCodes)
    {
        _userQueryService = userQueryService;
        _permissionCodes = permissionCodes;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = await _userQueryService.GetCurrentUserAsync(context.HttpContext.RequestAborted);

        if (!_permissionCodes.Any(requiredCode => user.PermissionCodes.Contains(requiredCode, StringComparer.Ordinal)))
        {
            throw new ForbiddenException("You have not permission to perform this action");
        }
    }
}
