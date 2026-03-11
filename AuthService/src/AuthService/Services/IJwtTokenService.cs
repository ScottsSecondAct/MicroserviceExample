using AuthService.Models;
using SharedLibrary.Enums;

namespace AuthService.Services;

public interface IJwtTokenService
{
  string GenerateJwtToken(User user, UserRole role);
}
