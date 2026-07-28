using Microsoft.EntityFrameworkCore;
using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Infrastructure.Persistence;

namespace TeamOrganization.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _dbContext;

    public UnitOfWork(AppDbContext appDbContext)
    {
        _dbContext = appDbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}