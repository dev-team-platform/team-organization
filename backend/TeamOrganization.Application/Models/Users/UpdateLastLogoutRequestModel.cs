namespace TeamOrganization.Application.Models.Users;

public class UpdateLastLogoutRequestModel
{
    public string IdentitySubject { get; set; } = null!;
    public DateTimeOffset LastLogoutAt { get; set; }
}