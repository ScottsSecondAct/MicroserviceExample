using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReportingService.Data;
using Testcontainers.PostgreSql;

namespace ReportingService.IntegrationTests;

public class ReportingServiceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync() => await _db.StartAsync();
    public new async Task DisposeAsync() => await _db.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:ReportingDbConnection", _db.GetConnectionString());

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ReportingDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<ReportingDbContext>(o => o.UseNpgsql(_db.GetConnectionString()));

            services.AddMassTransitTestHarness();
        });
    }
}
