using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Infrastructure.Persistence;

namespace TeamOrganization.Infrastructure.Repositories;

public class FileStorageRepository : GenericRepository<FileStorage>, IFileStorageRepository
{
    public FileStorageRepository(Serilog.ILogger logger, AppDbContext appDbContext)
        : base(logger, appDbContext)
    {
    }
}
