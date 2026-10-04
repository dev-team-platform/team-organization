using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Infrastructure.Persistence;

namespace TeamOrganization.Infrastructure.Repositories;

public class AuditLogRepository : GenericRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(Serilog.ILogger logger, AppDbContext appDbContext)
        : base(logger, appDbContext)
    {
    }
}
