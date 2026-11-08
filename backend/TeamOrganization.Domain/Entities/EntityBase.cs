namespace TeamOrganization.Domain.Entities;

public class EntityBase
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public Guid? UpdatedById { get; set; }
    public User? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
