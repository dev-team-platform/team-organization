using TeamOrganization.Api.Dtos.Common;
using TeamOrganization.Application.Models.Users;

namespace TeamOrganization.Api.Dtos.V1.Users;

public class GetAllUsersRequest : FilterRequest<GetAllUsersResponseItem>
{
    public override FilterQueryMapRequest<GetAllUsersResponseItem> GetFilterRequestMap()
    {
        return new FilterQueryMapRequest<GetAllUsersResponseItem>()
            .Map("employeeCode", x => x.EmployeeCode)
            .Map("username", x => x.Username)
            .Map("email", x => x.Email)
            .Map("firstName", x => x.FirstName)
            .Map("lastName", x => x.LastName)
            .Map("displayName", x => x.DisplayName)
            .Map("status", x => x.Status)
            .Map("lastLoginAt", x => x.LastLoginAt);
    }

    public static GetAllUsersRequestModel ToModel(GetAllUsersRequest request)
    {
        return new GetAllUsersRequestModel
        {
            CurrentPage = request.CurrentPage,
            ItemsPerPage = request.ItemsPerPage,
            SearchGlobalText = request.SearchGlobalText,
            FilterGroup = request.BuildFilterGroup(),
            FilterCriteria = request.BuildFilterCriteria(),
            SortFields = request.BuildSortFields()
        };
    }
}
