namespace AuthService.Services;

/// <summary>
/// Development stub: logs the invite link rather than sending a real email.
/// Replace with an SMTP or third-party email provider implementation for production.
/// </summary>
public class EmailService : IEmailService
{
  private readonly ILogger<EmailService> _logger;
  private readonly IConfiguration _configuration;

  public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
  {
    _logger = logger;
    _configuration = configuration;
  }

  public Task SendInviteEmailAsync(string toEmail, string inviteToken)
  {
    var frontendUrl = _configuration["InviteSettings:FrontendUrl"] ?? "http://localhost:3000";
    var acceptUrl = $"{frontendUrl}/accept-invite?token={inviteToken}";

    _logger.LogInformation(
        "INVITE EMAIL (dev stub) — To: {Email} | Accept URL: {AcceptUrl}",
        toEmail,
        acceptUrl);

    return Task.CompletedTask;
  }
}
