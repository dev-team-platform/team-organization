using TeamOrganization.Api.Dtos.Common;
using TeamOrganization.Application.Models.Users;

namespace TeamOrganization.Api.Dtos.V1.Users;

public class ChangeRoleConfirmationResponse
{
    public Guid UserId { get; set; }
    public string RoleCode { get; set; } = null!;
    public NavigationResponse? UpdatedBy { get; set; }

    public static ChangeRoleConfirmationResponse FromModel(ChangeRoleConfirmationResponseModel model) => new()
    {
        UserId = model.UserId,
        RoleCode = model.RoleCode,
        UpdatedBy = NavigationResponse.FromModel(model.UpdatedBy)
    };
}
