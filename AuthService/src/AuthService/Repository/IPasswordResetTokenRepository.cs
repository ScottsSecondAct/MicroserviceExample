using AuthService.Models;

namespace AuthService.Repository;

public interface IPasswordResetTokenRepository
{
  Task AddAsync(PasswordResetToken token);
  Task<PasswordResetToken?> GetByTokenAsync(string token);
  Task UpdateAsync(PasswordResetToken token);
}
