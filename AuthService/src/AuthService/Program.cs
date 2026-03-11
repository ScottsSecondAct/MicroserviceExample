using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using AuthService.Data;
using AuthService.Services;
using AuthService.Repository;
using MassTransit;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Register DbContext
builder.Services.AddDbContext<AuthDbContext>(options =>
{
  var connectionString = builder.Configuration.GetConnectionString("AuthDbConnection");
  options.UseNpgsql(connectionString);
});

// Configure JWT authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings.GetValue<string>("SecretKey") ?? throw new ArgumentNullException("SecretKey", "SecretKey cannot be null");

// Register services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRegistrationService, RegistationService>();
builder.Services.AddScoped<ILoginService, LoginService>();

// Typed HttpClient for role fetch (sync call on login)
builder.Services.AddHttpClient<IUserRoleClient, UserRoleClient>(client =>
{
  client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:UserManagementService"]
      ?? throw new ArgumentNullException("ServiceUrls:UserManagementService"));
});

// MassTransit + RabbitMQ (publish-only from AuthService)
builder.Services.AddMassTransit(x =>
{
  x.UsingRabbitMq((ctx, cfg) =>
  {
    cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
    {
      h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
      h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
    });
    cfg.ConfigureEndpoints(ctx);
  });
});

builder.Services.AddAuthentication(options =>
{
  options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
  options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
  options.TokenValidationParameters = new TokenValidationParameters
  {
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
    ValidAudience = jwtSettings.GetValue<string>("Audience"),
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
  };
});

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("AuthDbConnection") ?? string.Empty,
        name: "auth-db");

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("auth-service"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

var app = builder.Build();

// Apply schema on startup (for Docker / fresh environments)
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
  db.Database.EnsureCreated();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.UseSwagger();
app.UseSwaggerUI();

app.Run();

public partial class Program { }
