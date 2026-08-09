namespace TeamOrganization.Infrastructure.Options;

public class AppAntiforgeryOptions
{
    public const string SectionName = "Antiforgery";
    public string HeaderName { get; set; } = null!;
    public string CookieName { get; set; } = null!;
    public string RequestTokenCookieName { get; set; } = null!;
    public string SameSite { get; set; } = null!;
    public string SecurePolicy { get; set; } = null!;
    public string Path { get; set; } = null!;
}
