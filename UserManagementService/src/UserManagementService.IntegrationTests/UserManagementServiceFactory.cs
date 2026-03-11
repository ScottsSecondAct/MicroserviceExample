using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using UserManagementService.Consumers;
using UserManagementService.Data;

namespace UserManagementService.IntegrationTests;

public class UserManagementServiceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
  private readonly PostgreSqlContainer _db = new PostgreSqlBuilder().Build();

  public async Task InitializeAsync() => await _db.StartAsync();
  public new async Task DisposeAsync() => await _db.DisposeAsync();

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseSetting("ConnectionStrings:UserManagementDbConnection", _db.GetConnectionString());

    builder.ConfigureServices(services =>
    {
      var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<UserManagementDbContext>));
      if (descriptor != null) services.Remove(descriptor);

      services.AddDbContext<UserManagementDbContext>(o => o.UseNpgsql(_db.GetConnectionString()));

      services.AddMassTransitTestHarness(x => x.AddConsumer<UserRegisteredConsumer>());
    });
  }
}
