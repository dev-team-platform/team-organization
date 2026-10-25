using System.ComponentModel.DataAnnotations;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Enums.Users;
using TeamOrganization.Domain.Utils;

namespace TeamOrganization.Api.Dtos.V1.Users;

public class UpdateUserRequest
{
    [Required(ErrorMessage = "First Name is required.")]
    [MaxLength(100, ErrorMessage = "First Name cannot be longer than 100 characters.")]
    public string FirstName { get; set; } = null!;

    [MaxLength(100, ErrorMessage = "Last Name cannot be longer than 100 characters.")]
    public string? LastName { get; set; }

    [Required(ErrorMessage = "Display Name is required.")]
    [MaxLength(200, ErrorMessage = "Display Name cannot be longer than 200 characters.")]
    public string DisplayName { get; set; } = null!;

    [Required(ErrorMessage = "Employee Code is required.")]
    [MaxLength(10, ErrorMessage = "Employee Code cannot be longer than 10 characters.")]
    public string EmployeeCode { get; set; } = null!;

    [Required(ErrorMessage = "Gender is required.")]
    [MaxLength(20, ErrorMessage = "Gender cannot be longer than 20 characters.")]
    public string Gender { get; set; } = null!;
    public DateOnly? BirthDate { get; set; }

    public static UpdateUserRequestModel ToModel(UpdateUserRequest request)
    {
        return new UpdateUserRequestModel
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            DisplayName = request.DisplayName,
            EmployeeCode = request.EmployeeCode,
            Gender = EnumUtils.Parse<UserGender>(request.Gender),
            BirthDate = request.BirthDate
        };
    }
}
