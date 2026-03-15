using OpenTelemetry.Exporter;
using Serilog.Enrichers.OpenTelemetry;
using ApiGateway.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, config) => config
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("serviceId", "api-gateway")
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.Seq(ctx.Configuration["Seq:Url"] ?? "http://seq:5341")
    .Enrich.WithOpenTelemetryTraceId()
    .Enrich.WithOpenTelemetrySpanId());

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings.GetValue<string>("SecretKey")
    ?? throw new ArgumentNullException("SecretKey", "SecretKey cannot be null");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
});

var rateLimitSettings = builder.Configuration.GetSection("RateLimiting");
var perIpLimit = rateLimitSettings.GetValue("PerIpPermitLimit", 100);
var perUserLimit = rateLimitSettings.GetValue("PerUserPermitLimit", 200);
var windowSeconds = rateLimitSettings.GetValue("WindowSeconds", 60);
var retryAfterSeconds = rateLimitSettings.GetValue("RetryAfterSeconds", 60).ToString(NumberFormatInfo.InvariantInfo);

builder.Services.AddRateLimiter(options =>
{
  options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

  options.OnRejected = async (context, cancellationToken) =>
  {
    var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterTimeSpan)
        ? ((int)retryAfterTimeSpan.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo)
        : retryAfterSeconds;

    context.HttpContext.Response.Headers.RetryAfter = retryAfter;
    await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", cancellationToken);
  };

  // Chained global limiter: every request must satisfy both the per-IP and per-user buckets.
  options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
      // Per-IP limiter — applies to all requests
      PartitionedRateLimiter.Create<HttpContext, string>(context =>
      {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"ip:{ip}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
              PermitLimit = perIpLimit,
              Window = TimeSpan.FromSeconds(windowSeconds),
              QueueLimit = 0
            });
      }),
      // Per-user limiter — applies only to authenticated requests (falls through for anonymous)
      PartitionedRateLimiter.Create<HttpContext, string>(context =>
      {
        var userId = context.User.FindFirstValue("UserId")
            ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
          return RateLimitPartition.GetNoLimiter("anonymous");

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"user:{userId}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
              PermitLimit = perUserLimit,
              Window = TimeSpan.FromSeconds(windowSeconds),
              QueueLimit = 0
            });
      })
  );
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(',')
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(policy =>
      policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// Health checks — aggregate downstream service health
builder.Services.AddHealthChecks()
    .AddUrlGroup(
        new Uri($"{builder.Configuration["ReverseProxy:Clusters:auth-cluster:Destinations:auth-service:Address"]}/health"),
        name: "auth-service")
    .AddUrlGroup(
        new Uri($"{builder.Configuration["ReverseProxy:Clusters:users-cluster:Destinations:user-management-service:Address"]}/health"),
        name: "user-management-service")
    .AddUrlGroup(
        new Uri($"{builder.Configuration["ReverseProxy:Clusters:contacts-cluster:Destinations:contact-service:Address"]}/health"),
        name: "contact-service")
    .AddUrlGroup(
        new Uri($"{builder.Configuration["ReverseProxy:Clusters:accounts-cluster:Destinations:account-service:Address"]}/health"),
        name: "account-service");

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("api-gateway"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();
app.MapHealthChecks("/health");
app.MapReverseProxy();

app.Run();
