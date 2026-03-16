namespace UserManagementService.Services;

public interface IEmailService
{
  Task SendInviteEmailAsync(string email, string inviteToken);
}
