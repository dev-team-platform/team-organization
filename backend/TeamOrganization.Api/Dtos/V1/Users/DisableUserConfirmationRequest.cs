using System.ComponentModel.DataAnnotations;
using TeamOrganization.Application.Models.Users;

namespace TeamOrganization.Api.Dtos.V1.Users;

public class DisableUserConfirmationRequest
{
    [Required]
    [StringLength(10, MinimumLength = 10)]
    [RegularExpression("^[a-zA-Z0-9]{10}$")]
    public string Otp { get; set; } = null!;

    public static DisableUserConfirmationRequestModel ToModel(DisableUserConfirmationRequest request, Guid userId) => new()
    {
        UserId = userId,
        Otp = request.Otp
    };
}
