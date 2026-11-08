using TeamOrganization.Application.Models.Users;

namespace TeamOrganization.Api.Dtos.V1.Users;

public class DisableUserRequest
{
    public static DisableUserRequestModel ToModel(DisableUserRequest request, Guid userId) => new()
    {
        UserId = userId
    };
}
