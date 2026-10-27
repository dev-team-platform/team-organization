namespace TeamOrganization.Application.Models.Users;

public class ChangeRoleResponseModel
{
    public Guid UserId { get; set; }
    public string RoleCode { get; set; } = null!;
    public int OtpExpiresInMinutes { get; set; }
}
