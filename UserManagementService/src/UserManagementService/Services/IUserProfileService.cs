using SharedLibrary.DTOs;
using SharedLibrary.Enums;

namespace UserManagementService.Services;

public interface IUserProfileService
{
  Task<ServiceResult> CreateUserProfileAsync(CreateUserProfileRequest request);
  Task<ServiceResult> GetUserProfileAsync(Guid userId);
  Task<ServiceResult> GetUserRoleAsync(Guid userId);
  Task<ServiceResult> GetTeamAsync();
  Task<ServiceResult> GetAllUsersAsync();
  Task<ServiceResult> UpdateUserRoleAsync(Guid userId, UserRole role, Guid actorUserId);
  Task<ServiceResult> SetUserActiveAsync(Guid userId, bool isActive, Guid actorUserId);
  Task<ServiceResult> ResendInviteAsync(Guid userId, Guid actorUserId);
}
