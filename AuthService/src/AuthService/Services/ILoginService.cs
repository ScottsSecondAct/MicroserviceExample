using AuthService.Models.DTOs;

namespace AuthService.Services;

public interface ILoginService
{
  Task<ServiceResult> LoginAsync(LoginRequest request);
}
