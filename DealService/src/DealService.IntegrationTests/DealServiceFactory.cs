using DealService.Consumers;
using DealService.Data;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using WireMock.Server;

namespace DealService.IntegrationTests;

public class DealServiceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
  private readonly PostgreSqlContainer _db = new PostgreSqlBuilder().Build();
  public WireMockServer AccountServiceMock { get; private set; } = null!;
  public WireMockServer ContactServiceMock { get; private set; } = null!;

  public async Task InitializeAsync()
  {
    await _db.StartAsync();
    AccountServiceMock = WireMockServer.Start();
    ContactServiceMock = WireMockServer.Start();
  }

  public new async Task DisposeAsync()
  {
    AccountServiceMock.Stop();
    ContactServiceMock.Stop();
    await _db.DisposeAsync();
  }

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseSetting("ConnectionStrings:DealDbConnection", _db.GetConnectionString());

    builder.ConfigureServices(services =>
    {
      var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DealDbContext>));
      if (descriptor != null) services.Remove(descriptor);

      services.AddDbContext<DealDbContext>(o => o.UseNpgsql(_db.GetConnectionString()));

      services.AddMassTransitTestHarness(x => x.AddConsumer<ContactDeletedConsumer>());
    });

    builder.UseSetting("ServiceUrls:AccountService", AccountServiceMock.Url);
    builder.UseSetting("ServiceUrls:ContactService", ContactServiceMock.Url);
  }
}
