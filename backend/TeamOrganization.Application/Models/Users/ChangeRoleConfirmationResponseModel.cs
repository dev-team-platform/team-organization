using TeamOrganization.Application.Models.Common;

namespace TeamOrganization.Application.Models.Users;

public class ChangeRoleConfirmationResponseModel
{
    public Guid UserId { get; set; }
    public string RoleCode { get; set; } = null!;
    public NavigationResponseModel? UpdatedBy { get; set; }
}
