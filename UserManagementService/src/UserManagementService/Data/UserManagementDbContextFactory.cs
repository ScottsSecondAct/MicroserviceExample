using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UserManagementService.Data;

public class UserManagementDbContextFactory : IDesignTimeDbContextFactory<UserManagementDbContext>
{
    public UserManagementDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<UserManagementDbContext>()
            .UseNpgsql("Host=localhost;Database=user-management-db;Username=postgres;Password=postgres")
            .Options;
        return new UserManagementDbContext(options);
    }
}
