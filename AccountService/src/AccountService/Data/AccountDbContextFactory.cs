using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AccountService.Data;

public class AccountDbContextFactory : IDesignTimeDbContextFactory<AccountDbContext>
{
    public AccountDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AccountDbContext>()
            .UseNpgsql("Host=localhost;Database=account-db;Username=postgres;Password=postgres")
            .Options;
        return new AccountDbContext(options, httpContextAccessor: null);
    }
}
