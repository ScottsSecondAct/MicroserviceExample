namespace AuthService.Services;

public interface IInviteService
{
  Task<ServiceResult> CreateInviteAsync(string email, Guid adminUserId);
  Task<ServiceResult> AcceptInviteAsync(string token, string password);
}
