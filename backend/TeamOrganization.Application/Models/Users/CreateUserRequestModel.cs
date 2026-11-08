using TeamOrganization.Domain.Enums.Users;

namespace TeamOrganization.Application.Models.Users;

public class CreateUserRequestModel
{
    public string IdentitySubject { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string? LastName { get; set; }
    public string DisplayName { get; set; } = null!;
    public string EmployeeCode { get; set; } = null!;
    public string RoleCode { get; set; } = null!;
    public UserGender Gender { get; set; } = UserGender.Male;
    public DateOnly? BirthDate { get; set; }
}
