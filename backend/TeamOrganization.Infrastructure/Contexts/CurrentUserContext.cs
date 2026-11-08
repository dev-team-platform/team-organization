using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TeamOrganization.Application.Interfaces.Contexts;
using TeamOrganization.Domain.Exceptions;

namespace TeamOrganization.Infrastructure.Contexts;

public class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string IdentitySubject =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue("sub")
        ?? throw new UnauthorizedException("User is not authenticated.");
}