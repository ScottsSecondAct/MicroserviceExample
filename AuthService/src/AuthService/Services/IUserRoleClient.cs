using SharedLibrary.Enums;

namespace AuthService.Services;

public interface IUserRoleClient
{
  Task<UserRole> GetRoleAsync(Guid userId);
}
