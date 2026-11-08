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
    private readonly bool _requiredAll;

    public PermissionsAuthorizationFilter(
        IUserQueryService userQueryService,
        string[] permissionCodes,
        bool requiredAll = false)
    {
        _userQueryService = userQueryService;
        _permissionCodes = permissionCodes;
        _requiredAll = requiredAll;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var hasPermission = await _userQueryService.HasPermissionsAsync(
            [.. _permissionCodes],
            _requiredAll,
            context.HttpContext.RequestAborted);

        if (!hasPermission)
        {
            throw new ForbiddenException("You have not permission to perform this action");
        }
    }
}
