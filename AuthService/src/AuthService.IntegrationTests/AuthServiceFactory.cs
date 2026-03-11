using AuthService.Data;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using WireMock.Server;

namespace AuthService.IntegrationTests;

public class AuthServiceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
  private readonly PostgreSqlContainer _db = new PostgreSqlBuilder().Build();
  public WireMockServer UmsMock { get; private set; } = null!;

  public const string TestJwtSecret = "integration-test-secret-key-must-be-long-enough-32-chars";
  public const string TestJwtIssuer = "https://localhost";
  public const string TestJwtAudience = "YourAppUsers";

  public async Task InitializeAsync()
  {
    await _db.StartAsync();
    UmsMock = WireMockServer.Start();
  }

  public new async Task DisposeAsync()
  {
    UmsMock.Stop();
    await _db.DisposeAsync();
  }

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseSetting("JwtSettings:SecretKey", TestJwtSecret);
    builder.UseSetting("JwtSettings:Issuer", TestJwtIssuer);
    builder.UseSetting("JwtSettings:Audience", TestJwtAudience);
    builder.UseSetting("ServiceUrls:UserManagementService", UmsMock.Url);

    builder.UseSetting("ConnectionStrings:AuthDbConnection", _db.GetConnectionString());

    builder.ConfigureServices(services =>
    {
      var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AuthDbContext>));
      if (descriptor != null) services.Remove(descriptor);

      services.AddDbContext<AuthDbContext>(o => o.UseNpgsql(_db.GetConnectionString()));

      services.AddMassTransitTestHarness();
    });
  }
}
