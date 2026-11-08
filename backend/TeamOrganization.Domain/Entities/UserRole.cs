namespace TeamOrganization.Domain.Entities;

public class UserRole : EntityBase
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
}
