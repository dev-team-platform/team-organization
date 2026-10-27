namespace TeamOrganization.Application.Models.Users;

public class ChangeRoleRequestModel
{
    public Guid UserId { get; set; }
    public string RoleCode { get; set; } = null!;
}
