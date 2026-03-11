using DealService.Consumers;
using DealService.Data;
using DealService.Repository;
using DealService.Services;
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

builder.Services.AddDbContext<DealDbContext>(options =>
  options.UseNpgsql(builder.Configuration.GetConnectionString("DealDbConnection")));

builder.Services.AddScoped<IDealRepository, DealRepository>();
builder.Services.AddScoped<IDealService, DealsService>();

builder.Services.AddHttpClient<IAccountClient, AccountClient>(client =>
{
  client.BaseAddress = new Uri(
    builder.Configuration["ServiceUrls:AccountService"] ?? "http://localhost:5243");
});

builder.Services.AddHttpClient<IContactClient, ContactClient>(client =>
{
  client.BaseAddress = new Uri(
    builder.Configuration["ServiceUrls:ContactService"] ?? "http://localhost:5273");
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddMassTransit(x =>
{
  x.AddConsumer<ContactDeletedConsumer>();

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

builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DealDbConnection") ?? string.Empty,
        name: "deal-db");

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("deal-service"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<DealDbContext>();
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
