using AuthService.Data;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repository;

public class RefreshTokenRepository : IRefreshTokenRepository
{
  private readonly AuthDbContext _context;

  public RefreshTokenRepository(AuthDbContext context)
  {
    _context = context;
  }

  public async Task AddAsync(RefreshToken token)
  {
    await _context.RefreshTokens.AddAsync(token);
    await _context.SaveChangesAsync();
  }

  public async Task<RefreshToken?> GetByTokenAsync(string token)
  {
    return await _context.RefreshTokens
        .Include(r => r.User)
        .FirstOrDefaultAsync(r => r.Token == token);
  }

  public async Task RevokeAsync(RefreshToken token)
  {
    token.IsRevoked = true;
    _context.RefreshTokens.Update(token);
    await _context.SaveChangesAsync();
  }
}
