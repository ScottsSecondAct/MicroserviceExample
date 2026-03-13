using Microsoft.EntityFrameworkCore;
using UserManagementService.Consumers;
using UserManagementService.Data;
using UserManagementService.Repository;
using UserManagementService.Services;
using MassTransit;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<UserManagementDbContext>(options =>
  options.UseNpgsql(builder.Configuration.GetConnectionString("UserManagementDbConnection")));

builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// MassTransit + RabbitMQ (consume UserRegistered events)
builder.Services.AddMassTransit(x =>
{
  x.AddConsumer<UserRegisteredConsumer>();

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

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("UserManagementDbConnection") ?? string.Empty,
        name: "user-management-db");

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

  // Seed default admin profile if not present
  var adminConfig = app.Configuration.GetSection("DefaultAdmin");
  var adminIdStr = adminConfig["UserId"];
  var adminEmail = adminConfig["Email"];
  var adminDisplayName = adminConfig["DisplayName"] ?? "Default Admin";
  if (!string.IsNullOrEmpty(adminIdStr) && !string.IsNullOrEmpty(adminEmail))
  {
    var adminId = Guid.Parse(adminIdStr);
    if (!db.UserProfiles.Any(u => u.UserId == adminId))
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
      db.SaveChanges();
    }
  }
}

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
