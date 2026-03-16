using AuthService.Models;

namespace AuthService.Repository;

public interface IInviteTokenRepository
{
  Task AddAsync(InviteToken inviteToken);
  Task<InviteToken?> GetByTokenAsync(string token);
  Task UpdateAsync(InviteToken inviteToken);
}
