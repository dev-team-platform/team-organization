using TeamOrganization.Api.Dtos.Common;
using TeamOrganization.Application.Models.Users;

namespace TeamOrganization.Api.Dtos.V1.Users;

public class CreateNewUserResponse
{
    public Guid Id { get; set; }
    public string IdentitySubject { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string? LastName { get; set; }
    public string DisplayName { get; set; } = null!;
    public string EmployeeCode { get; set; } = null!;
    public NavigationResponse? CreatedBy { get; set; }
    public NavigationResponse? UpdatedBy { get; set; }

    public static CreateNewUserResponse FromModel(CreateUserResponseModel model)
    {
        return new CreateNewUserResponse
        {
            Id = model.Id,
            IdentitySubject = model.IdentitySubject,
            Username = model.Username,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            DisplayName = model.DisplayName,
            EmployeeCode = model.EmployeeCode,
            CreatedBy = NavigationResponse.FromModel(model.CreatedBy),
            UpdatedBy = NavigationResponse.FromModel(model.UpdatedBy)
        };
    }
}