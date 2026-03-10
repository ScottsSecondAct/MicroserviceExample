using AuthService.Data;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repository;

public class UserRepository : IUserRepository
{
  private readonly AuthDbContext _context;

  public UserRepository(AuthDbContext context)
  {
    _context = context;
  }

  public async Task AddUserAsync(User user)
  {
    await _context.Users.AddAsync(user);
    await _context.SaveChangesAsync();
  }

  public async Task<User?> GetUserByEmailAsync(string email)
  {
    return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
  }

  public async Task<User?> GetUserByIdAsync(Guid userId)
  {
    return await _context.Users.FindAsync(userId);
  }

  public async Task UpdateUserAsync(User user)
  {
    _context.Users.Update(user);
    await _context.SaveChangesAsync();
  }

  public async Task DeleteUserAsync(Guid userId)
  {
    var user = await _context.Users.FindAsync(userId);
    if (user != null)
    {
      _context.Users.Remove(user);
      await _context.SaveChangesAsync();
    }
  }
}
