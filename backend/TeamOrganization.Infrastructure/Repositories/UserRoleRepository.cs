using Microsoft.EntityFrameworkCore;
using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Infrastructure.Persistence;

namespace TeamOrganization.Infrastructure.Repositories;

public class UserRoleRepository : GenericRepository<UserRole>, IUserRoleRepository
{
    public UserRoleRepository(Serilog.ILogger logger, AppDbContext appDbContext)
        : base(logger, appDbContext)
    {
    }

    
}
