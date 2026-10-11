using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Enums.Users;
using TeamOrganization.Domain.Utils;

namespace TeamOrganization.Api.Dtos.V1.Users;

public class CreateUserRequest
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string? LastName { get; set; }
    public string DisplayName { get; set; } = null!;
    public string EmployeeCode { get; set; } = null!;
    public string RoleCode { get; set; } = null!;
    public string Gender { get; set; } = null!;
    public DateOnly? BirthDate { get; set; }

    public static CreateUserRequestModel ToModel(CreateUserRequest request)
    {
        return new CreateUserRequestModel
        {
            Username = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            DisplayName = request.DisplayName,
            EmployeeCode = request.EmployeeCode,
            RoleCode = request.RoleCode,
            Gender = EnumUtils.Parse<UserGender>(request.Gender),
            BirthDate = request.BirthDate
        };
    }
}
