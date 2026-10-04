using TeamOrganization.Api.Dtos.Common;
using TeamOrganization.Application.Models.Users;

namespace TeamOrganization.Api.Dtos.V1.Users;

public class GetAllUsersResponse : FilterResponse<GetAllUsersResponseItem>
{
    public static GetAllUsersResponse FromModel(GetAllUsersResponseModel model)
    {
        return new GetAllUsersResponse
        {
            Items = [.. model.Items.Select(GetAllUsersResponseItem.FromModel)],
            CurrentPage = model.CurrentPage,
            ItemsPerPage = model.ItemsPerPage,
            TotalItems = model.TotalItems,
            TotalPages = model.TotalPages
        };
    }
}

public class GetAllUsersResponseItem
{
    public Guid Id { get; init; }
    public string EmployeeCode { get; init; } = null!;
    public string Username { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string? LastName { get; init; }
    public string DisplayName { get; init; } = null!;
    public string? AvatarUrl { get; init; }
    public string Status { get; init; } = null!;
    public DateTimeOffset? LastLoginAt { get; init; }

    public static GetAllUsersResponseItem FromModel(GetAllUsersResponseModelItem modelItem)
    {
        return new GetAllUsersResponseItem
        {
            Id = modelItem.Id,
            EmployeeCode = modelItem.EmployeeCode,
            Username = modelItem.Username,
            Email = modelItem.Email,
            FirstName = modelItem.FirstName,
            LastName = modelItem.LastName,
            DisplayName = modelItem.DisplayName,
            AvatarUrl = modelItem.AvatarUrl,
            Status = modelItem.Status.ToString(),
            LastLoginAt = modelItem.LastLoginAt
        };
    }
}
