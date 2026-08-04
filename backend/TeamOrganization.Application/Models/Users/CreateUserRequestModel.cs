namespace TeamOrganization.Application.Models.Users;

public class CreateUserRequestModel
{
    public string IdentitySubject { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string EmployeeCode { get; set; } = null!;
}