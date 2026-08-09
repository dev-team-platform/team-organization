namespace TeamOrganization.Domain.Entities;

public class Role : EntityBase
{
    public string DisplayName { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string? Description { get; set; }
}
