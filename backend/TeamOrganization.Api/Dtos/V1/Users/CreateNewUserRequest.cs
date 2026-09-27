namespace TeamOrganization.Api.Dtos.V1.Users;

public class CreateNewUserRequest
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string? LastName { get; set; }
    public string DisplayName { get; set; } = null!;
    public string EmployeeCode { get; set; } = null!;
    public string RoleId { get; set; } = null!;
}
