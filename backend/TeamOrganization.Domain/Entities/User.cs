namespace TeamOrganization.Domain.Entities;

public class User : EntityBase
{
    public string IdentitySubject { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string EmployeeCode { get; set; } = null!;
}