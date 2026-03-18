namespace AuthService.Services;

public class TenantResolver : ITenantResolver
{
  private readonly IConfiguration _configuration;

  public TenantResolver(IConfiguration configuration)
  {
    _configuration = configuration;
  }

  public Guid Resolve(HttpContext? context)
  {
    // Shared-cloud path: gateway injects X-Tenant-Id header from subdomain
    if (context != null &&
        context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerValue) &&
        Guid.TryParse(headerValue, out var tenantId))
    {
      return tenantId;
    }

    // Single-tenant fallback: use configured default
    var defaultId = _configuration["DefaultTenant:TenantId"];
    return Guid.TryParse(defaultId, out var defaultTenantId) ? defaultTenantId : Guid.Empty;
  }
}
