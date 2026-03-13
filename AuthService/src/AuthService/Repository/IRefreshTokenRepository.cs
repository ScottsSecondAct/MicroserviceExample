using AuthService.Models;

namespace AuthService.Repository;

public interface IRefreshTokenRepository
{
  Task AddAsync(RefreshToken token);
  Task<RefreshToken?> GetByTokenAsync(string token);
  Task RevokeAsync(RefreshToken token);
}
