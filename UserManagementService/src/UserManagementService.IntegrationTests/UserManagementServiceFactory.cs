using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Testcontainers.PostgreSql;
using UserManagementService.Consumers;
using UserManagementService.Data;

namespace UserManagementService.IntegrationTests;

public class UserManagementServiceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
  private readonly PostgreSqlContainer _db = new PostgreSqlBuilder().Build();

  public const string TestJwtSecret = "integration-test-secret-key-minimum-32-bytes!";
  public const string TestJwtIssuer = "test-issuer";
  public const string TestJwtAudience = "test-audience";

  public async Task InitializeAsync() => await _db.StartAsync();
  public new async Task DisposeAsync() => await _db.DisposeAsync();

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseSetting("ConnectionStrings:UserManagementDbConnection", _db.GetConnectionString());
    builder.UseSetting("JwtSettings:SecretKey", TestJwtSecret);
    builder.UseSetting("JwtSettings:Issuer", TestJwtIssuer);
    builder.UseSetting("JwtSettings:Audience", TestJwtAudience);

    builder.ConfigureServices(services =>
    {
      var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<UserManagementDbContext>));
      if (descriptor != null) services.Remove(descriptor);

      services.AddDbContext<UserManagementDbContext>(o => o.UseNpgsql(_db.GetConnectionString()));

      services.AddMassTransitTestHarness(x => x.AddConsumer<UserRegisteredConsumer>());
    });
  }

  public string CreateAdminJwt(Guid? userId = null)
  {
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
      new Claim(ClaimTypes.Role, "Admin"),
      new Claim("UserId", (userId ?? Guid.NewGuid()).ToString()),
      new Claim(JwtRegisteredClaimNames.Sub, "admin@test.com"),
      new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    };

    var token = new JwtSecurityToken(
      issuer: TestJwtIssuer,
      audience: TestJwtAudience,
      claims: claims,
      expires: DateTime.UtcNow.AddHours(1),
      signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
  }
}
