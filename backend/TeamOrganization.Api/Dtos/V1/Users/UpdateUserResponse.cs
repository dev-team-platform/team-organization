using TeamOrganization.Api.Dtos.Common;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Utils;

namespace TeamOrganization.Api.Dtos.V1.Users;

public class UpdateUserResponse
{
    public Guid Id { get; set; }
    public string IdentitySubject { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string? LastName { get; set; }
    public string DisplayName { get; set; } = null!;
    public string EmployeeCode { get; set; } = null!;
    public string Gender { get; set; } = null!;
    public DateOnly? BirthDate { get; set; }
    public NavigationResponse? UpdatedBy { get; set; }

    public static UpdateUserResponse FromModel(UpdateUserResponseModel model)
    {
        return new UpdateUserResponse
        {
            Id = model.Id,
            IdentitySubject = model.IdentitySubject,
            Username = model.Username,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            DisplayName = model.DisplayName,
            EmployeeCode = model.EmployeeCode,
            Gender = EnumUtils.ToString(model.Gender),
            BirthDate = model.BirthDate,
            UpdatedBy = NavigationResponse.FromModel(model.UpdatedBy)
        };
    }
}
