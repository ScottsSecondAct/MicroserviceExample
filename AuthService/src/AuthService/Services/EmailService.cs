using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AuthService.Services;

public class EmailService : IEmailService
{
  private readonly ILogger<EmailService> _logger;
  private readonly IConfiguration _configuration;

  public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
  {
    _logger = logger;
    _configuration = configuration;
  }

  public async Task SendInviteEmailAsync(string toEmail, string inviteToken)
  {
    var frontendUrl = _configuration["InviteSettings:FrontendUrl"] ?? "http://localhost:3000";
    var acceptUrl = $"{frontendUrl}/accept-invite?token={inviteToken}";

    var smtpHost = _configuration["Smtp:Host"];
    if (string.IsNullOrWhiteSpace(smtpHost))
    {
      _logger.LogInformation(
          "INVITE EMAIL (dev stub — no SMTP configured) — To: {Email} | Accept URL: {AcceptUrl}",
          toEmail,
          acceptUrl);
      return;
    }

    var smtpPort = int.TryParse(_configuration["Smtp:Port"], out var port) ? port : 587;
    var smtpUser = _configuration["Smtp:Username"] ?? string.Empty;
    var smtpPass = _configuration["Smtp:Password"] ?? string.Empty;
    var fromAddress = _configuration["Smtp:FromAddress"] ?? smtpUser;
    var fromName = _configuration["Smtp:FromName"] ?? "CRM System";
    var useSsl = string.Equals(_configuration["Smtp:SecureSocketOptions"], "SslOnConnect",
        StringComparison.OrdinalIgnoreCase);

    var message = new MimeMessage();
    message.From.Add(new MailboxAddress(fromName, fromAddress));
    message.To.Add(new MailboxAddress(string.Empty, toEmail));
    message.Subject = "You have been invited";

    message.Body = new BodyBuilder
    {
      HtmlBody = $"""
        <p>You have been invited to join. Click the link below to accept:</p>
        <p><a href="{acceptUrl}">{acceptUrl}</a></p>
        <p>This link expires in {_configuration["InviteSettings:TokenExpiryHours"] ?? "48"} hours.</p>
        """,
      TextBody = $"You have been invited. Accept at: {acceptUrl}"
    }.ToMessageBody();

    using var client = new SmtpClient();
    var socketOptions = useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
    await client.ConnectAsync(smtpHost, smtpPort, socketOptions);

    if (!string.IsNullOrEmpty(smtpUser))
      await client.AuthenticateAsync(smtpUser, smtpPass);

    await client.SendAsync(message);
    await client.DisconnectAsync(true);

    _logger.LogInformation("Invite email sent to {Email}", toEmail);
  }
}
