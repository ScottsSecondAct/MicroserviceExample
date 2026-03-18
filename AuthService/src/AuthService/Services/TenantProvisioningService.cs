using AuthService.Data;
using AuthService.Models;
using AuthService.Models.DTOs;
using MassTransit;
using SharedLibrary.DTOs;
using SharedLibrary.Messaging.Events;

namespace AuthService.Services;

public class TenantProvisioningService : ITenantProvisioningService
{
  private readonly AuthDbContext _db;
  private readonly IPasswordService _passwordService;
  private readonly IPasswordPolicyService _passwordPolicyService;
  private readonly IPublishEndpoint _publishEndpoint;

  public TenantProvisioningService(
      AuthDbContext db,
      IPasswordService passwordService,
      IPasswordPolicyService passwordPolicyService,
      IPublishEndpoint publishEndpoint)
  {
    _db = db;
    _passwordService = passwordService;
    _passwordPolicyService = passwordPolicyService;
    _publishEndpoint = publishEndpoint;
  }

  public async Task<ServiceResult> ProvisionAsync(ProvisionTenantRequest request)
  {
    var (isValid, errors) = _passwordPolicyService.Validate(request.AdminPassword);
    if (!isValid)
      return ServiceResult.Failure(string.Join(" ", errors), 400);

    if (_db.Tenants.Any(t => t.Slug == request.Slug))
      return ServiceResult.Failure($"A tenant with slug '{request.Slug}' already exists.", 409);

    var tenant = new Tenant
    {
      TenantId = Guid.NewGuid(),
      Slug = request.Slug,
      DisplayName = request.DisplayName,
      CreatedAt = DateTime.UtcNow
    };

    _db.Tenants.Add(tenant);

    var adminUserId = Guid.NewGuid();
    var adminUser = new User
    {
      UserId = adminUserId,
      Email = request.AdminEmail,
      Username = request.AdminUsername,
      TenantId = tenant.TenantId,
      PasswordHash = _passwordService.HashPassword(request.AdminPassword)
    };

    _db.Users.Add(adminUser);
    await _db.SaveChangesAsync();

    await _publishEndpoint.Publish(new UserRegistered
    {
      UserId = adminUserId,
      Email = request.AdminEmail,
      Username = request.AdminUsername,
      TenantId = tenant.TenantId
    });

    return ServiceResult.Success(new TenantDto
    {
      TenantId = tenant.TenantId,
      Slug = tenant.Slug,
      DisplayName = tenant.DisplayName
    }, "Tenant provisioned successfully.", 201);
  }
}
