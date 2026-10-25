using TeamOrganization.Domain.Enums.Users;

namespace TeamOrganization.Application.Models.Users;

public class UpdateUserRequestModel
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = null!;
    public string? LastName { get; set; }
    public string DisplayName { get; set; } = null!;
    public string EmployeeCode { get; set; } = null!;
    public UserGender Gender { get; set; } = UserGender.Male;
    public DateOnly? BirthDate { get; set; }
}
