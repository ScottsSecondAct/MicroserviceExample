using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.DTOs;
using SharedLibrary.Enums;
using UserManagementService.Data;
using UserManagementService.Models;
using UserManagementService.Models.DTOs;
using UserManagementService.Repository;

namespace UserManagementService.Services;

public class UserProfileService : IUserProfileService
{
  private readonly IUserProfileRepository _repository;
  private readonly IEmailService _emailService;
  private readonly IAuditLogService _auditLogService;
  private readonly UserManagementDbContext _db;

  public UserProfileService(IUserProfileRepository repository, IEmailService emailService, IAuditLogService auditLogService, UserManagementDbContext db)
  {
    _repository = repository;
    _emailService = emailService;
    _auditLogService = auditLogService;
    _db = db;
  }

  private async Task<Guid> GetDefaultTenantIdAsync()
  {
    var tenant = await _db.Tenants.OrderBy(t => t.CreatedAt).FirstOrDefaultAsync();
    return tenant?.TenantId ?? Guid.Empty;
  }

  public async Task<ServiceResult> CreateUserProfileAsync(CreateUserProfileRequest request)
  {
    var existing = await _repository.GetByIdAsync(request.UserId);
    if (existing != null)
    {
      // Profile was pre-created by UserInvitedConsumer; clear the pending state.
      existing.InvitePendingAt = null;
      existing.InviteToken = null;
      existing.IsActive = true;
      await _repository.UpdateAsync(existing);
      return ServiceResult.Success(new CreateUserProfileResponse
      {
        UserId = existing.UserId,
        Role = existing.Role
      }, "User profile activated successfully.", 200);
    }

    var tenantId = request.TenantId ?? await GetDefaultTenantIdAsync();

    var profile = new UserProfile
    {
      UserId = request.UserId,
      Email = request.Email,
      TenantId = tenantId,
      Role = UserRole.Member,
      DisplayName = request.Email,
      CreatedAt = DateTime.UtcNow
    };

    await _repository.AddAsync(profile);

    return ServiceResult.Success(new CreateUserProfileResponse
    {
      UserId = profile.UserId,
      Role = profile.Role
    }, "User profile created successfully.", 201);
  }

  public async Task<ServiceResult> GetUserProfileAsync(Guid userId)
  {
    var profile = await _repository.GetByIdAsync(userId);
    if (profile == null)
      return ServiceResult.Failure("User profile not found.", 404);

    return ServiceResult.Success(profile);
  }

  public async Task<ServiceResult> GetUserRoleAsync(Guid userId)
  {
    var profile = await _repository.GetByIdAsync(userId);
    if (profile == null)
      return ServiceResult.Failure("User profile not found.", 404);

    return ServiceResult.Success(new UserRoleResponse
    {
      UserId = profile.UserId,
      Role = profile.Role,
      IsActive = profile.IsActive
    });
  }

  public async Task<ServiceResult> GetTeamAsync()
  {
    var profiles = await _repository.GetAllAsync();
    var team = profiles.Select(p => new TeamMemberResponse
    {
      UserId = p.UserId,
      DisplayName = p.DisplayName,
      Role = p.Role
    }).ToList();

    return ServiceResult.Success(team);
  }

  public async Task<ServiceResult> GetAllUsersAsync()
  {
    var profiles = await _repository.GetAllAsync();
    var users = profiles.Select(p => new AdminUserResponse
    {
      UserId = p.UserId,
      Email = p.Email,
      DisplayName = p.DisplayName,
      Role = p.Role,
      IsActive = p.IsActive,
      CreatedAt = p.CreatedAt,
      InvitePendingAt = p.InvitePendingAt
    }).ToList();

    return ServiceResult.Success(users);
  }

  public async Task<ServiceResult> UpdateUserRoleAsync(Guid userId, UserRole role, Guid actorUserId)
  {
    if (role == UserRole.Unassigned)
      return ServiceResult.Failure("Cannot set role to Unassigned. Use Member, SalesRep, Manager, or Admin.");

    var profile = await _repository.GetByIdAsync(userId);
    if (profile == null)
      return ServiceResult.Failure("User profile not found.", 404);

    var previousRole = profile.Role;
    profile.Role = role;
    await _repository.UpdateAsync(profile);

    await _auditLogService.LogActionAsync(
      AuditAction.RoleChanged,
      actorUserId,
      userId,
      $"Role changed from {previousRole} to {role}");

    return ServiceResult.Success(new AdminUserResponse
    {
      UserId = profile.UserId,
      Email = profile.Email,
      DisplayName = profile.DisplayName,
      Role = profile.Role,
      IsActive = profile.IsActive,
      CreatedAt = profile.CreatedAt,
      InvitePendingAt = profile.InvitePendingAt
    });
  }

  public async Task<ServiceResult> SetUserActiveAsync(Guid userId, bool isActive, Guid actorUserId)
  {
    var profile = await _repository.GetByIdAsync(userId);
    if (profile == null)
      return ServiceResult.Failure("User profile not found.", 404);

    profile.IsActive = isActive;
    await _repository.UpdateAsync(profile);

    var action = isActive ? AuditAction.AccountActivated : AuditAction.AccountDeactivated;
    await _auditLogService.LogActionAsync(action, actorUserId, userId);

    return ServiceResult.Success(new AdminUserResponse
    {
      UserId = profile.UserId,
      Email = profile.Email,
      DisplayName = profile.DisplayName,
      Role = profile.Role,
      IsActive = profile.IsActive,
      CreatedAt = profile.CreatedAt,
      InvitePendingAt = profile.InvitePendingAt
    });
  }

  public async Task<ServiceResult> ResendInviteAsync(Guid userId, Guid actorUserId)
  {
    var profile = await _repository.GetByIdAsync(userId);
    if (profile == null)
      return ServiceResult.Failure("User profile not found.", 404);

    if (profile.InviteToken == null)
      return ServiceResult.Failure("User does not have a pending invite.");

    var newToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    profile.InviteToken = newToken;
    profile.InvitePendingAt = DateTime.UtcNow;
    await _repository.UpdateAsync(profile);

    await _emailService.SendInviteEmailAsync(profile.Email, newToken);
    await _auditLogService.LogActionAsync(AuditAction.InviteSent, actorUserId, userId);

    return ServiceResult.Success(new ResendInviteResponse
    {
      UserId = profile.UserId,
      Email = profile.Email,
      InviteSentAt = profile.InvitePendingAt.Value
    });
  }
}
