using AuthService.Models.DTOs;

namespace AuthService.Services;

public interface ITenantProvisioningService
{
  Task<ServiceResult> ProvisionAsync(ProvisionTenantRequest request);
}
