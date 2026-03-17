namespace AuthService.Services;

public interface IPasswordPolicyService
{
  (bool IsValid, IReadOnlyList<string> Errors) Validate(string password);
}
