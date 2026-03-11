using Microsoft.EntityFrameworkCore;
using UserManagementService.Data;
using UserManagementService.Models;

namespace UserManagementService.Repository;

public class UserProfileRepository : IUserProfileRepository
{
  private readonly UserManagementDbContext _context;

  public UserProfileRepository(UserManagementDbContext context)
  {
    _context = context;
  }

  public async Task<UserProfile?> GetByIdAsync(Guid userId) =>
    await _context.UserProfiles.FindAsync(userId);

  public async Task<UserProfile?> GetByEmailAsync(string email) =>
    await _context.UserProfiles.FirstOrDefaultAsync(u => u.Email == email);

  public async Task<List<UserProfile>> GetAllAsync() =>
    await _context.UserProfiles.ToListAsync();

  public async Task AddAsync(UserProfile profile)
  {
    _context.UserProfiles.Add(profile);
    await _context.SaveChangesAsync();
  }

  public async Task UpdateAsync(UserProfile profile)
  {
    _context.UserProfiles.Update(profile);
    await _context.SaveChangesAsync();
  }

  public async Task DeleteAsync(Guid userId)
  {
    var profile = await _context.UserProfiles.FindAsync(userId);
    if (profile != null)
    {
      _context.UserProfiles.Remove(profile);
      await _context.SaveChangesAsync();
    }
  }
}
