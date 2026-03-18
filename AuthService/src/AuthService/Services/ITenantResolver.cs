using Microsoft.AspNetCore.Http;

namespace AuthService.Services;

public interface ITenantResolver
{
  Guid Resolve(HttpContext? context);
}
