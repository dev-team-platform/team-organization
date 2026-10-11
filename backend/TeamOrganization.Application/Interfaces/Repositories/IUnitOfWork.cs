namespace TeamOrganization.Application.Interfaces.Repositories;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(string actorId, CancellationToken cancellationToken = default);
    Task<int> SaveChangesWithoutAuditAsync(CancellationToken cancellationToken = default);
}
