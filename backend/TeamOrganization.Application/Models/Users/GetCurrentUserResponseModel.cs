namespace TeamOrganization.Application.Models.Users;

public class GetCurrentUserResponseModel
{
    public Guid Id { get; set; }
    public string EmployeeCode { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string? LastName { get; set; }
    public string DisplayName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset? LastLogoutAt { get; set; }
    public string RoleName { get; set; } = null!;
    public List<string> PermissionCodes { get; set; } = [];
}