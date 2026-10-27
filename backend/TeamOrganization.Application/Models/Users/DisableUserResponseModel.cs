namespace TeamOrganization.Application.Models.Users;

public class DisableUserResponseModel
{
    public Guid UserId { get; set; }
    public int OtpExpiresInMinutes { get; set; }
}
