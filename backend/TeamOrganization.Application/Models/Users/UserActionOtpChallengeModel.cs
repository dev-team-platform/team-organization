namespace TeamOrganization.Application.Models.Users;

public class UserActionOtpChallengeModel
{
    public string OtpHash { get; set; } = null!;
    public Guid RequestedById { get; set; }
    public string? RoleCode { get; set; }
}
