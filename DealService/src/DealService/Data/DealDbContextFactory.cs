using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DealService.Data;

public class DealDbContextFactory : IDesignTimeDbContextFactory<DealDbContext>
{
    public DealDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DealDbContext>()
            .UseNpgsql("Host=localhost;Database=deal-db;Username=postgres;Password=postgres")
            .Options;
        return new DealDbContext(options, httpContextAccessor: null);
    }
}
