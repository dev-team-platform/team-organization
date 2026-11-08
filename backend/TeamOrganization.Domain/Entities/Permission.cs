namespace TeamOrganization.Domain.Entities;

public class Permission : EntityBase
{
    public string Code { get; set; } = null!;
    public string? Description { get; set; }
}
