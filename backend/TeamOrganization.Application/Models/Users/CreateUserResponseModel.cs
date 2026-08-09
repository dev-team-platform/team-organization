using TeamOrganization.Application.Models.Common;

namespace TeamOrganization.Application.Models.Users;

public class CreateUserResponseModel
{
    public Guid Id { get; set; }
    public string IdentitySubject { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string EmployeeCode { get; set; } = null!;
    public NavigationResponseModel? CreatedBy { get; set; }
    public NavigationResponseModel? UpdatedBy { get; set; }
}