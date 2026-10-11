using TeamOrganization.Application.Models.Common;

namespace TeamOrganization.Application.Models.Users;

public class GetAllUsersRequestModel : FilterQuery<GetAllUsersResponseModelItem>
{
    public override FilterQueryMap<GetAllUsersResponseModelItem> GetFilterQueryMap()
    {
        return new FilterQueryMap<GetAllUsersResponseModelItem>()
            .Map("id", x => x.Id)
            .Map("employeeCode", x => x.EmployeeCode)
            .Map("username", x => x.Username)
            .Map("email", x => x.Email)
            .Map("firstName", x => x.FirstName)
            .Map("lastName", x => x.LastName)
            .Map("displayName", x => x.DisplayName)
            .Map("roleName", x => x.RoleName)
            .Map("status", x => x.Status)
            .Map("gender", x => x.Gender)
            .Map("birthDate", x => x.BirthDate)
            .Map("lastLoginAt", x => x.LastLoginAt);
    }
}
