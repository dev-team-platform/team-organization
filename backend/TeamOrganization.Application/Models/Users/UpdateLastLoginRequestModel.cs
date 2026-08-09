namespace TeamOrganization.Application.Models.Users;

public class UpdateLastLoginRequestModel
{
    public string IdentitySubject { get; set; } = null!;
    public DateTimeOffset LastLoginAt { get; set; }
}