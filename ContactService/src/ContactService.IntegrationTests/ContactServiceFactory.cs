using ContactService.Data;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using WireMock.Server;

namespace ContactService.IntegrationTests;

public class ContactServiceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
  private readonly PostgreSqlContainer _db = new PostgreSqlBuilder().Build();
  public WireMockServer AccountServiceMock { get; private set; } = null!;

  public async Task InitializeAsync()
  {
    await _db.StartAsync();
    AccountServiceMock = WireMockServer.Start();
  }

  public new async Task DisposeAsync()
  {
    AccountServiceMock.Stop();
    await _db.DisposeAsync();
  }

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseSetting("ConnectionStrings:ContactDbConnection", _db.GetConnectionString());

    builder.ConfigureServices(services =>
    {
      var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ContactDbContext>));
      if (descriptor != null) services.Remove(descriptor);

      services.AddDbContext<ContactDbContext>(o => o.UseNpgsql(_db.GetConnectionString()));

      services.AddMassTransitTestHarness();
    });

    builder.UseSetting("ServiceUrls:AccountService", AccountServiceMock.Url);
  }
}
