using Microsoft.EntityFrameworkCore;
using TeamOrganization.Domain.Entities;

namespace TeamOrganization.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
}