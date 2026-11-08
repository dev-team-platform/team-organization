using TeamOrganization.Application.Models.Common;
using TeamOrganization.Domain.Enums.Users;

namespace TeamOrganization.Application.Models.Users;

public class DisableUserConfirmationResponseModel
{
    public Guid UserId { get; set; }
    public UserStatus Status { get; set; }
    public NavigationResponseModel? UpdatedBy { get; set; }
}
