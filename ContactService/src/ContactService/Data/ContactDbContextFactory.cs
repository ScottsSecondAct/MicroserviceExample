using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ContactService.Data;

public class ContactDbContextFactory : IDesignTimeDbContextFactory<ContactDbContext>
{
    public ContactDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ContactDbContext>()
            .UseNpgsql("Host=localhost;Database=contact-db;Username=postgres;Password=postgres")
            .Options;
        return new ContactDbContext(options, httpContextAccessor: null);
    }
}
