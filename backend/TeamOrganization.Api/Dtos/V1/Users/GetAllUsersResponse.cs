using TeamOrganization.Api.Dtos.Common;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Utils;

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
    public string? RoleName { get; init; }
    public string? AvatarUrl { get; init; }
    public string Status { get; init; } = null!;
    public string Gender { get; init; } = null!;
    public DateOnly? BirthDate { get; init; }
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
            RoleName = modelItem.RoleName,
            AvatarUrl = modelItem.AvatarUrl,
            Status = EnumUtils.ToString(modelItem.Status),
            Gender = EnumUtils.ToString(modelItem.Gender),
            BirthDate = modelItem.BirthDate,
            LastLoginAt = modelItem.LastLoginAt
        };
    }
}
