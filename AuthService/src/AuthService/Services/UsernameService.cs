using AuthService.Repository;

namespace AuthService.Services;

public class UsernameService : IUsernameService
{
  private readonly IUserRepository _userRepository;

  public UsernameService(IUserRepository userRepository)
  {
    _userRepository = userRepository;
  }

  public async Task<string> DeriveUniqueUsernameAsync(string email, Guid tenantId)
  {
    var prefix = email.Split('@')[0]
        .ToLowerInvariant()
        .Replace('.', '_')
        .Replace('+', '_');

    var candidate = prefix;
    var suffix = 2;
    while (await _userRepository.GetUserByUsernameAsync(tenantId, candidate) != null)
    {
      candidate = $"{prefix}{suffix++}";
    }

    return candidate;
  }
}
