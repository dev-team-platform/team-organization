using TeamOrganization.Application.Models.Users;

namespace TeamOrganization.Api.Dtos.V1.Users;

public class DisableUserResponse
{
    public Guid UserId { get; set; }
    public int OtpExpiresInMinutes { get; set; }

    public static DisableUserResponse FromModel(DisableUserResponseModel model) => new()
    {
        UserId = model.UserId,
        OtpExpiresInMinutes = model.OtpExpiresInMinutes
    };
}
