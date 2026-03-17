namespace AuthService.Services;

public interface IForgotPasswordService
{
  Task<ServiceResult> ForgotPasswordAsync(string email);
  Task<ServiceResult> ResetPasswordAsync(string token, string newPassword);
}
