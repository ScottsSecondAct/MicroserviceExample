namespace AuthService.Services;

public interface IChangePasswordService
{
  Task<ServiceResult> ChangePasswordAsync(Guid userId, string newPassword);
}
