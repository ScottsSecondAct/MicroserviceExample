namespace AuthService.Services;

public interface IRegistrationService
{
  Task<bool> ValidateEmailAsync(string email);

  Task<ServiceResult> RegisterUserAsync(string email, string password);
}
