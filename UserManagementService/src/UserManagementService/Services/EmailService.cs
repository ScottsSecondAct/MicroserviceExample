namespace UserManagementService.Services;

public class EmailService : IEmailService
{
  private readonly ILogger<EmailService> _logger;

  public EmailService(ILogger<EmailService> logger)
  {
    _logger = logger;
  }

  public Task SendInviteEmailAsync(string email, string inviteToken)
  {
    // Stub: log instead of sending a real email until an email provider is configured.
    _logger.LogInformation("Invite email queued for {Email} with token {InviteToken}", email, inviteToken);
    return Task.CompletedTask;
  }
}
