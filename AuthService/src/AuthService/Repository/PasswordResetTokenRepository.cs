using AuthService.Data;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repository;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
  private readonly AuthDbContext _context;

  public PasswordResetTokenRepository(AuthDbContext context)
  {
    _context = context;
  }

  public async Task AddAsync(PasswordResetToken token)
  {
    await _context.PasswordResetTokens.AddAsync(token);
    await _context.SaveChangesAsync();
  }

  public async Task<PasswordResetToken?> GetByTokenAsync(string token)
  {
    return await _context.PasswordResetTokens.FirstOrDefaultAsync(t => t.Token == token);
  }

  public async Task UpdateAsync(PasswordResetToken token)
  {
    _context.PasswordResetTokens.Update(token);
    await _context.SaveChangesAsync();
  }
}
