using AuthService.Data;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repository;

public class InviteTokenRepository : IInviteTokenRepository
{
  private readonly AuthDbContext _context;

  public InviteTokenRepository(AuthDbContext context)
  {
    _context = context;
  }

  public async Task AddAsync(InviteToken inviteToken)
  {
    await _context.InviteTokens.AddAsync(inviteToken);
    await _context.SaveChangesAsync();
  }

  public async Task<InviteToken?> GetByTokenAsync(string token)
  {
    return await _context.InviteTokens.FirstOrDefaultAsync(t => t.Token == token);
  }

  public async Task UpdateAsync(InviteToken inviteToken)
  {
    _context.InviteTokens.Update(inviteToken);
    await _context.SaveChangesAsync();
  }
}
