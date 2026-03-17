using OpenTelemetry.Exporter;
using Serilog.Enrichers.OpenTelemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using UserManagementService.Consumers;
using UserManagementService.Data;
using UserManagementService.Infrastructure;
using UserManagementService.Middleware;
using UserManagementService.Repository;
using UserManagementService.Services;
using MassTransit;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, config) => config
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("serviceId", "user-management-service")
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.Seq(ctx.Configuration["Seq:Url"] ?? "http://seq:5341")
    .Enrich.WithOpenTelemetryTraceId()
    .Enrich.WithOpenTelemetrySpanId());

builder.Services.AddControllers()
  .AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<UserManagementDbContext>(options =>
  options.UseNpgsql(builder.Configuration.GetConnectionString("UserManagementDbConnection")));

builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddSingleton<IEmailService, EmailService>();

// Configure JWT authentication (validates tokens issued by AuthService)
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings.GetValue<string>("SecretKey") ?? throw new ArgumentNullException("SecretKey", "SecretKey cannot be null");

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

builder.Services.AddAuthorization(options =>
{
  options.AddPolicy("admin", policy => policy.RequireRole("Admin"));
  options.AddPolicy("manager", policy => policy.RequireRole("Manager", "Admin"));
  options.AddPolicy("salesRep", policy => policy.RequireRole("SalesRep", "Manager", "Admin"));
  options.AddPolicy("member", policy => policy.RequireRole("Member", "SalesRep", "Manager", "Admin"));
});

// MassTransit + RabbitMQ (consume UserRegistered events)
builder.Services.AddMassTransit(x =>
{
  x.AddConsumer<UserRegisteredConsumer>();
  x.AddConsumer<UserInvitedConsumer>();

  x.UsingRabbitMq((ctx, cfg) =>
  {
    cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
    {
      h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
      h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
    });
    cfg.UseMessageRetry(r => r.Exponential(5,
      TimeSpan.FromSeconds(1),
      TimeSpan.FromSeconds(60),
      TimeSpan.FromSeconds(5)));
    cfg.ConfigureEndpoints(ctx);
  });
});

// Health checks
builder.Services.AddHttpClient();
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("UserManagementDbConnection") ?? string.Empty,
        name: "user-management-db")
    .AddCheck<DlqHealthCheck>("rabbitmq-dlq", tags: ["messaging"]);

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("user-management-service"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

var app = builder.Build();

// Apply schema on startup (for Docker / fresh environments)
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
  db.Database.EnsureCreated();

  // Ensure default admin profile exists and is always active with Admin role
  var adminConfig = app.Configuration.GetSection("DefaultAdmin");
  var adminIdStr = adminConfig["UserId"];
  var adminEmail = adminConfig["Email"];
  var adminDisplayName = adminConfig["DisplayName"] ?? "Default Admin";
  if (!string.IsNullOrEmpty(adminIdStr) && !string.IsNullOrEmpty(adminEmail))
  {
    var adminId = Guid.Parse(adminIdStr);
    var adminProfile = db.UserProfiles.FirstOrDefault(u => u.UserId == adminId);
    if (adminProfile == null)
    {
      db.UserProfiles.Add(new UserManagementService.Models.UserProfile
      {
        UserId = adminId,
        Email = adminEmail,
        DisplayName = adminDisplayName,
        Role = SharedLibrary.Enums.UserRole.Admin,
        CreatedAt = DateTime.UtcNow,
        IsActive = true
      });
    }
    else
    {
      adminProfile.Role = SharedLibrary.Enums.UserRole.Admin;
      adminProfile.IsActive = true;
    }
    db.SaveChanges();
  }
}

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
