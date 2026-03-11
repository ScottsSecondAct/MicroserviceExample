using AccountService.Data;
using AccountService.Repository;
using AccountService.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
  .AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AccountDbContext>(options =>
  options.UseNpgsql(builder.Configuration.GetConnectionString("AccountDbConnection")));

builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IAccountService, AccountsService>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// MassTransit + RabbitMQ (publish-only, no consumers)
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

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("AccountDbConnection") ?? string.Empty,
        name: "account-db");

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("account-service"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

var app = builder.Build();

// Apply schema on startup (for Docker / fresh environments)
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
  db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
