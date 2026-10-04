using TeamOrganization.Application.Models.Common;
using TeamOrganization.Domain.Enums;

namespace TeamOrganization.Application.Models.Users;

public class GetAllUsersResponseModel : FilterResult<GetAllUsersResponseModelItem>
{
    public static GetAllUsersResponseModel FromFilterResult(FilterResult<GetAllUsersResponseModelItem> filterResult)
    {
        return new GetAllUsersResponseModel
        {
            Items = filterResult.Items,
            CurrentPage = filterResult.CurrentPage,
            ItemsPerPage = filterResult.ItemsPerPage,
            TotalItems = filterResult.TotalItems,
            TotalPages = filterResult.TotalPages
        };
    }
}

public class GetAllUsersResponseModelItem
{
    public Guid Id { get; init; }
    public string EmployeeCode { get; init; } = null!;
    public string Username { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string? LastName { get; init; }
    public string DisplayName { get; init; } = null!;
    public string? AvatarUrl { get; init; }
    public UserStatus Status { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
}
