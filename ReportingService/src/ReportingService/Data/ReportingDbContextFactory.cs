using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ReportingService.Data;

public class ReportingDbContextFactory : IDesignTimeDbContextFactory<ReportingDbContext>
{
    public ReportingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseNpgsql("Host=localhost;Database=reporting-db;Username=postgres;Password=postgres")
            .Options;
        return new ReportingDbContext(options);
    }
}
