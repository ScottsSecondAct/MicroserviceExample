using SharedLibrary.DTOs;

namespace UserManagementService.Services;

public interface IUserProfileService
{
  Task<ServiceResult> CreateUserProfileAsync(CreateUserProfileRequest request);
  Task<ServiceResult> GetUserProfileAsync(Guid userId);
}
