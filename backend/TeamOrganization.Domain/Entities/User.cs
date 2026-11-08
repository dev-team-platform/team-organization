using TeamOrganization.Domain.Enums.Users;

namespace TeamOrganization.Domain.Entities;

public class User : EntityBase
{
    public string IdentitySubject { get; set; } = null!;
    public string EmployeeCode { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string? LastName { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public UserStatus Status { get; set; }
    public UserGender Gender { get; set; } = UserGender.Male;
    public DateOnly? BirthDate { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public AdditionalDataObject AdditionalData { get; set; } = new();
}

public class AdditionalDataObject
{
    public string? DisableOtp { get; set; }
    public DateTimeOffset? DisableOtpExpired { get; set; }
    public Guid? DisableOtpRequestedById { get; set; }
    public string? ChangeRoleOtp { get; set; }
    public DateTimeOffset? ChangeRoleOtpExpired { get; set; }
    public Guid? ChangeRoleOtpRequestedById { get; set; }
    public string? ChangeRoleOtpRoleCode { get; set; }
}
