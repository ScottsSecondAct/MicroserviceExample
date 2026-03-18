using OpenTelemetry.Exporter;
using Serilog.Enrichers.OpenTelemetry;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ReportingService.Consumers;
using ReportingService.Data;
using ReportingService.Infrastructure;
using ReportingService.Middleware;
using ReportingService.Models;
using Serilog;
using Serilog.Formatting.Compact;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, config) => config
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("serviceId", "reporting-service")
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.Seq(ctx.Configuration["Seq:Url"] ?? "http://seq:5341")
    .Enrich.WithOpenTelemetryTraceId()
    .Enrich.WithOpenTelemetrySpanId());

builder.Services.AddControllers()
  .AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ReportingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ReportingDbConnection")));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<DealCreatedConsumer>();
    x.AddConsumer<DealStageChangedConsumer>();
    x.AddConsumer<DealClosedConsumer>();
    x.AddConsumer<ActivityLoggedConsumer>();
    x.AddConsumer<ContactCreatedConsumer>();
    x.AddConsumer<ContactStatusChangedConsumer>();

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

builder.Services.AddHttpClient();
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("ReportingDbConnection") ?? string.Empty,
        name: "reporting-db")
    .AddCheck<DlqHealthCheck>("rabbitmq-dlq", tags: ["messaging"]);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("reporting-service"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
    db.Database.Migrate();

    // Seed pipeline stages so all 5 always appear in the projection even before any deals arrive
    var stages = new[] { "Prospecting", "Proposal", "Negotiation", "ClosedWon", "ClosedLost" };
    foreach (var stage in stages)
    {
        if (!db.PipelineProjections.Any(p => p.Stage == stage))
            db.PipelineProjections.Add(new PipelineProjection { Stage = stage });
    }

    // Seed contact funnel statuses
    var statuses = new[] { "Lead", "Prospect", "Customer", "Churned" };
    foreach (var status in statuses)
    {
        if (!db.ContactFunnelProjections.Any(c => c.Status == status))
            db.ContactFunnelProjections.Add(new ContactFunnelProjection { Status = status });
    }

    db.SaveChanges();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
