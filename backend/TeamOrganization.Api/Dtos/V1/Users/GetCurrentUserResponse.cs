using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Utils;

namespace TeamOrganization.Api.Dtos.V1.Users;

public class GetCurrentUserResponse
{
    public Guid Id { get; set; }
    public string EmployeeCode { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string? LastName { get; set; }
    public string DisplayName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string Gender { get; set; } = null!;
    public DateOnly? BirthDate { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public string RoleName { get; set; } = null!;
    public List<string> Permissions { get; set; } = [];

    public static GetCurrentUserResponse FromModel(GetCurrentUserResponseModel model)
    {
        return new GetCurrentUserResponse
        {
            Id = model.Id,
            EmployeeCode = model.EmployeeCode,
            Username = model.Username,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            DisplayName = model.DisplayName,
            Gender = EnumUtils.ToString(model.Gender),
            BirthDate = model.BirthDate,
            LastLoginAt = model.LastLoginAt,
            RoleName = model.RoleName,
            Permissions = model.PermissionCodes,
        };
    }
}
