using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TeamOrganization.Api.Authentication;
using TeamOrganization.Infrastructure.Options;

namespace TeamOrganization.Api.Controllers.V1;

[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("1.0")]
[ApiController]
public sealed class AuthenticationController : ControllerBase
{
    private readonly IAntiforgery _antiforgery;
    private readonly IOptions<AppAntiforgeryOptions> _antiforgeryOptions;

    public AuthenticationController(IAntiforgery antiforgery,
        IOptions<AppAntiforgeryOptions> antiforgeryOptions)
    {
        _antiforgery = antiforgery;
        _antiforgeryOptions = antiforgeryOptions;
    }

    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl = "/")
    {
        var safeReturnUrl = IsLocalUrl(returnUrl)
            ? returnUrl
            : "/";

        var properties = new AuthenticationProperties
        {
            RedirectUri = safeReturnUrl
        };

        return Challenge(properties, AuthenticationSchemes.Keycloak);
    }

    [HttpGet("csrf")]
    public IActionResult GetCsrfToken()
    {
        var settings = _antiforgeryOptions.Value;

        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

        Response.Cookies.Append(
            settings.RequestTokenCookieName,
            tokens.RequestToken!,
            new CookieOptions
            {
                HttpOnly = false,
                Path = settings.Path,
                SameSite = Enum.Parse<SameSiteMode>(
                    settings.SameSite,
                    ignoreCase: true),
                Secure = Enum.Parse<CookieSecurePolicy>(
                    settings.SecurePolicy,
                    ignoreCase: true) == CookieSecurePolicy.Always
            });

        return NoContent();
    }

    private static bool IsLocalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        return url[0] == '/'
            && (url.Length == 1
                || url[1] != '/'
                && url[1] != '\\');
    }
}