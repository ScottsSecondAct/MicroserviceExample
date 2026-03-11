using ContactService.Data;
using ContactService.Repository;
using ContactService.Services;
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

builder.Services.AddDbContext<ContactDbContext>(options =>
  options.UseNpgsql(builder.Configuration.GetConnectionString("ContactDbConnection")));

builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<IContactService, ContactsService>();

builder.Services.AddHttpClient<IAccountClient, AccountClient>(client =>
{
  client.BaseAddress = new Uri(
    builder.Configuration["ServiceUrls:AccountService"] ?? "http://localhost:5243");
});

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
        builder.Configuration.GetConnectionString("ContactDbConnection") ?? string.Empty,
        name: "contact-db");

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("contact-service"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

var app = builder.Build();

// Apply schema on startup (for Docker / fresh environments)
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<ContactDbContext>();
  db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
