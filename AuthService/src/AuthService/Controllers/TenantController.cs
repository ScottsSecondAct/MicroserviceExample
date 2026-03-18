using AuthService.Models.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("api/tenants")]
public class TenantController : ControllerBase
{
  private readonly ITenantProvisioningService _tenantProvisioningService;
  private readonly IConfiguration _configuration;
  private readonly ILogger<TenantController> _logger;

  public TenantController(
      ITenantProvisioningService tenantProvisioningService,
      IConfiguration configuration,
      ILogger<TenantController> logger)
  {
    _tenantProvisioningService = tenantProvisioningService;
    _configuration = configuration;
    _logger = logger;
  }

  /// <summary>
  /// Provision a new tenant and its admin user.
  /// Requires the X-Bootstrap-Secret header to match TenantProvisioning:BootstrapSecret in config.
  /// Used for shared-cloud (SaaS) onboarding; not needed for single-tenant deployments.
  /// </summary>
  [HttpPost("provision")]
  public async Task<IActionResult> Provision(
      [FromBody] ProvisionTenantRequest request,
      [FromHeader(Name = "X-Bootstrap-Secret")] string? bootstrapSecret)
  {
    var expectedSecret = _configuration["TenantProvisioning:BootstrapSecret"];
    if (string.IsNullOrEmpty(expectedSecret) || bootstrapSecret != expectedSecret)
    {
      _logger.LogWarning("Tenant provisioning rejected: invalid or missing bootstrap secret.");
      return Unauthorized(new { message = "Invalid or missing bootstrap secret." });
    }

    var result = await _tenantProvisioningService.ProvisionAsync(request);
    return result.IsSuccess
        ? StatusCode(result.StatusCode, result.Data)
        : StatusCode(result.StatusCode, new { message = result.Message });
  }
}
