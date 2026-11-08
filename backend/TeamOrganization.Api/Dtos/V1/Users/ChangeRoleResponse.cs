using TeamOrganization.Application.Models.Users;

namespace TeamOrganization.Api.Dtos.V1.Users;

public class ChangeRoleResponse
{
    public Guid UserId { get; set; }
    public string RoleCode { get; set; } = null!;
    public int OtpExpiresInMinutes { get; set; }

    public static ChangeRoleResponse FromModel(ChangeRoleResponseModel model) => new()
    {
        UserId = model.UserId,
        RoleCode = model.RoleCode,
        OtpExpiresInMinutes = model.OtpExpiresInMinutes
    };
}
