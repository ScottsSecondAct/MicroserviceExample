namespace AuthService.Services;

public interface IEmailService
{
  Task SendInviteEmailAsync(string toEmail, string inviteToken);
}
