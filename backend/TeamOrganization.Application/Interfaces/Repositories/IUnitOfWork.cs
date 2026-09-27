namespace TeamOrganization.Application.Interfaces.Repositories;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(Guid actorId, CancellationToken cancellationToken = default);
    Task<int> SaveChangesWithoutAuditAsync(CancellationToken cancellationToken = default);
}
