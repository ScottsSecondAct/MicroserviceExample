using AuthService.Models;

namespace AuthService.Services;
public interface IJwtTokenService
{
  string GenerateJwtToken(User user);
}
