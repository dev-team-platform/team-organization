namespace TeamOrganization.Application.Models.Users;

public class ChangeRoleConfirmationRequestModel
{
    public Guid UserId { get; set; }
    public string Otp { get; set; } = null!;
}
