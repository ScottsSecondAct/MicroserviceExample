using AuthService.Models;

namespace AuthService.Repository;

public interface IUserRepository
{
  Task AddUserAsync(User user);
  Task<User?> GetUserByEmailAsync(string email);
  Task<User?> GetUserByIdAsync(Guid userId);
  Task<User?> GetUserByUsernameAsync(Guid tenantId, string username);
  Task<Guid> GetDefaultTenantIdAsync();
  Task UpdateUserAsync(User user);
  Task DeleteUserAsync(Guid userId);
}
