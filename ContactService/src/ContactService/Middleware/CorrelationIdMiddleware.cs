using System.Diagnostics;
using System.Security.Claims;
using Serilog.Context;

namespace ContactService.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Activity.Current?.TraceId.ToString()
            ?? context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.Response.Headers["X-Correlation-ID"] = correlationId;

        var userId = context.User.FindFirstValue("UserId")
            ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "anonymous";

        using (LogContext.PushProperty("correlationId", correlationId))
        using (LogContext.PushProperty("userId", userId))
        {
            await next(context);
        }
    }
}
