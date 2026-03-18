using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ActivityService.Data;

public class ActivityDbContextFactory : IDesignTimeDbContextFactory<ActivityDbContext>
{
    public ActivityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ActivityDbContext>()
            .UseNpgsql("Host=localhost;Database=activity-db;Username=postgres;Password=postgres")
            .Options;
        return new ActivityDbContext(options, httpContextAccessor: null);
    }
}
