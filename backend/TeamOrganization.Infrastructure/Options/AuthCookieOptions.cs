namespace TeamOrganization.Infrastructure.Options;

public class AuthCookieOptions
{
    public const string SectionName = "AuthCookieOptions";

    public string CookieName { get; set; } = null!;
    public bool HttpOnly { get; set; }
    public string SecurePolicy { get; set; } = null!;
    public string SameSite { get; set; } = null!;
    public string Path { get; set; } = null!;
}