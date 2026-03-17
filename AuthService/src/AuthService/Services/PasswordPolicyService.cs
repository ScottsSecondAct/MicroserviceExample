using AuthService.Configuration;
using Microsoft.Extensions.Options;

namespace AuthService.Services;

public class PasswordPolicyService : IPasswordPolicyService
{
  private readonly PasswordPolicy _policy;

  public PasswordPolicyService(IOptions<PasswordPolicy> policy)
  {
    _policy = policy.Value;
  }

  public (bool IsValid, IReadOnlyList<string> Errors) Validate(string password)
  {
    var errors = new List<string>();

    if (string.IsNullOrEmpty(password) || password.Length < _policy.MinimumLength)
      errors.Add($"Password must be at least {_policy.MinimumLength} characters long.");

    if (_policy.RequireUppercase && !password.Any(char.IsUpper))
      errors.Add("Password must contain at least one uppercase letter.");

    if (_policy.RequireLowercase && !password.Any(char.IsLower))
      errors.Add("Password must contain at least one lowercase letter.");

    if (_policy.RequireDigit && !password.Any(char.IsDigit))
      errors.Add("Password must contain at least one digit.");

    if (_policy.RequireSpecialCharacter && !password.Any(c => _policy.SpecialCharacters.Contains(c)))
      errors.Add("Password must contain at least one special character.");

    return (errors.Count == 0, errors);
  }
}
