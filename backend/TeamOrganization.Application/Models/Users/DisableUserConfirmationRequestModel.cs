namespace TeamOrganization.Application.Models.Users;

public class DisableUserConfirmationRequestModel
{
    public Guid UserId { get; set; }
    public string Otp { get; set; } = null!;
}
