using AuthService.Models.DTOs;

namespace AuthService.Services;

public interface IUserRoleClient
{
  Task<UserRoleResponse> GetRoleAsync(Guid userId);
}
