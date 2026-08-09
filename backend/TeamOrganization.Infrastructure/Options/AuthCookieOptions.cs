namespace TeamOrganization.Infrastructure.Options;

public class AuthCookieOptions
{
    public const string SectionName = "Authentication:Cookie";

    public string CookieName { get; set; } = null!;
    public bool HttpOnly { get; set; }
    public string SecurePolicy { get; set; } = null!;
    public string SameSite { get; set; } = null!;
    public string Path { get; set; } = null!;
    public TimeSpan ExpireTimeSpan { get; set; }
    public bool SlidingExpiration { get; set; }
}