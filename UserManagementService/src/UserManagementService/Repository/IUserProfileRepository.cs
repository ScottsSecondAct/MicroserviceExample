using UserManagementService.Models;

namespace UserManagementService.Repository;

public interface IUserProfileRepository
{
  Task<UserProfile?> GetByIdAsync(Guid userId);
  Task<UserProfile?> GetByEmailAsync(string email);
  Task AddAsync(UserProfile profile);
  Task UpdateAsync(UserProfile profile);
  Task DeleteAsync(Guid userId);
}
