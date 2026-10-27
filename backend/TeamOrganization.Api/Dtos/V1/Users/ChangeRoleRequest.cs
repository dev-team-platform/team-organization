using System.ComponentModel.DataAnnotations;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Constants.Uam;

namespace TeamOrganization.Api.Dtos.V1.Users;

public class ChangeRoleRequest
{
    [Required(ErrorMessage = "Role Code is required.")]
    [AllowedValues([RC.Admin, RC.Employee], ErrorMessage = "Invalid Role Code.")]
    public string RoleCode { get; set; } = null!;

    public static ChangeRoleRequestModel ToModel(ChangeRoleRequest request, Guid userId) => new()
    {
        UserId = userId,
        RoleCode = request.RoleCode
    };
}
