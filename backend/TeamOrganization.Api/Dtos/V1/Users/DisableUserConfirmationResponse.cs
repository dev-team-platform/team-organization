using TeamOrganization.Api.Dtos.Common;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Utils;

namespace TeamOrganization.Api.Dtos.V1.Users;

public class DisableUserConfirmationResponse
{
    public Guid UserId { get; set; }
    public string Status { get; set; } = null!;
    public NavigationResponse? UpdatedBy { get; set; }

    public static DisableUserConfirmationResponse FromModel(DisableUserConfirmationResponseModel model) => new()
    {
        UserId = model.UserId,
        Status = EnumUtils.ToString(model.Status),
        UpdatedBy = NavigationResponse.FromModel(model.UpdatedBy)
    };
}
