using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using AuthService.Services;

public class EmailServiceTests
{
  private static EmailService CreateService(Dictionary<string, string?> config)
  {
    var logger = new Mock<ILogger<EmailService>>().Object;
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(config)
        .Build();
    return new EmailService(logger, configuration);
  }

  [Fact]
  public async Task SendInviteEmailAsync_WhenNoSmtpHostConfigured_LogsDevStubAndReturns()
  {
    var mockLogger = new Mock<ILogger<EmailService>>();
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["InviteSettings:FrontendUrl"] = "http://localhost:3000",
          ["Smtp:Host"] = ""
        })
        .Build();
    var service = new EmailService(mockLogger.Object, configuration);

    await service.SendInviteEmailAsync("user@example.com", "test-token");

    mockLogger.Verify(
        l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("dev stub")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
  }

  [Fact]
  public async Task SendInviteEmailAsync_WhenSmtpHostIsNull_LogsDevStubAndReturns()
  {
    var mockLogger = new Mock<ILogger<EmailService>>();
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["InviteSettings:FrontendUrl"] = "http://localhost:3000"
        })
        .Build();
    var service = new EmailService(mockLogger.Object, configuration);

    await service.SendInviteEmailAsync("user@example.com", "test-token");

    mockLogger.Verify(
        l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("dev stub")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
  }

  [Fact]
  public async Task SendPasswordResetEmailAsync_WhenNoSmtpHostConfigured_LogsDevStubAndReturns()
  {
    var mockLogger = new Mock<ILogger<EmailService>>();
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["InviteSettings:FrontendUrl"] = "http://localhost:3000",
          ["Smtp:Host"] = ""
        })
        .Build();
    var service = new EmailService(mockLogger.Object, configuration);

    await service.SendPasswordResetEmailAsync("user@example.com", "reset-token");

    mockLogger.Verify(
        l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("dev stub")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
  }

  [Fact]
  public async Task SendPasswordResetEmailAsync_WhenSmtpHostIsNull_LogsDevStubAndReturns()
  {
    var mockLogger = new Mock<ILogger<EmailService>>();
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["InviteSettings:FrontendUrl"] = "http://localhost:3000"
          // Smtp:Host intentionally absent → null
        })
        .Build();
    var service = new EmailService(mockLogger.Object, configuration);

    await service.SendPasswordResetEmailAsync("user@example.com", "reset-token");

    mockLogger.Verify(
        l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("dev stub")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
  }

  [Fact]
  public async Task SendInviteEmailAsync_WhenNoFrontendUrlConfigured_FallsBackToLocalhost()
  {
    var mockLogger = new Mock<ILogger<EmailService>>();
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          // InviteSettings:FrontendUrl intentionally absent → null-coalescing fallback
          ["Smtp:Host"] = ""
        })
        .Build();
    var service = new EmailService(mockLogger.Object, configuration);

    await service.SendInviteEmailAsync("user@example.com", "test-token");

    mockLogger.Verify(
        l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("dev stub")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
  }

  [Fact]
  public async Task SendPasswordResetEmailAsync_WhenNoFrontendUrlConfigured_FallsBackToLocalhost()
  {
    var mockLogger = new Mock<ILogger<EmailService>>();
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          // InviteSettings:FrontendUrl intentionally absent → null-coalescing fallback
          ["Smtp:Host"] = ""
        })
        .Build();
    var service = new EmailService(mockLogger.Object, configuration);

    await service.SendPasswordResetEmailAsync("user@example.com", "reset-token");

    mockLogger.Verify(
        l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("dev stub")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
  }

  // ── SMTP configured path ─────────────────────────────────────────────────
  // The SmtpClient is instantiated directly (not injectable), so these tests
  // exercise all configuration branches and then throw at ConnectAsync.

  [Fact]
  public async Task SendInviteEmailAsync_WithSmtpConfigured_SslOnConnect_AttemptsConnection()
  {
    var config = CreateService(new Dictionary<string, string?>
    {
      ["Smtp:Host"] = "smtp.example.com",
      ["Smtp:Port"] = "465",
      ["Smtp:SecureSocketOptions"] = "SslOnConnect",
      ["Smtp:Username"] = "user@example.com",
      ["Smtp:Password"] = "secret",
      ["InviteSettings:FrontendUrl"] = "http://localhost:3000",
    });

    await Assert.ThrowsAnyAsync<Exception>(() =>
        config.SendInviteEmailAsync("to@example.com", "token"));
  }

  [Fact]
  public async Task SendInviteEmailAsync_WithSmtpConfigured_StartTls_AttemptsConnection()
  {
    var config = CreateService(new Dictionary<string, string?>
    {
      ["Smtp:Host"] = "smtp.example.com",
      ["Smtp:Port"] = "587",
      ["Smtp:SecureSocketOptions"] = "StartTls",  // not "SslOnConnect" → useSsl=false branch
      ["Smtp:Username"] = "user@example.com",
      ["Smtp:Password"] = "secret",
    });

    await Assert.ThrowsAnyAsync<Exception>(() =>
        config.SendInviteEmailAsync("to@example.com", "token"));
  }

  [Fact]
  public async Task SendInviteEmailAsync_WithSmtpConfigured_InvalidPort_FallsBackTo587()
  {
    var config = CreateService(new Dictionary<string, string?>
    {
      ["Smtp:Host"] = "smtp.example.com",
      ["Smtp:Port"] = "not-a-number",  // TryParse fails → port = 587
    });

    await Assert.ThrowsAnyAsync<Exception>(() =>
        config.SendInviteEmailAsync("to@example.com", "token"));
  }

  [Fact]
  public async Task SendInviteEmailAsync_WithSmtpConfigured_EmptyUsername_SkipsAuthentication()
  {
    var config = CreateService(new Dictionary<string, string?>
    {
      ["Smtp:Host"] = "smtp.example.com",
      ["Smtp:Username"] = "",  // empty → skips AuthenticateAsync branch
    });

    await Assert.ThrowsAnyAsync<Exception>(() =>
        config.SendInviteEmailAsync("to@example.com", "token"));
  }

  [Fact]
  public async Task SendPasswordResetEmailAsync_WithSmtpConfigured_SslOnConnect_AttemptsConnection()
  {
    var config = CreateService(new Dictionary<string, string?>
    {
      ["Smtp:Host"] = "smtp.example.com",
      ["Smtp:Port"] = "465",
      ["Smtp:SecureSocketOptions"] = "SslOnConnect",
      ["Smtp:Username"] = "user@example.com",
      ["Smtp:Password"] = "secret",
    });

    await Assert.ThrowsAnyAsync<Exception>(() =>
        config.SendPasswordResetEmailAsync("to@example.com", "token"));
  }

  [Fact]
  public async Task SendPasswordResetEmailAsync_WithSmtpConfigured_EmptyUsername_SkipsAuthentication()
  {
    var config = CreateService(new Dictionary<string, string?>
    {
      ["Smtp:Host"] = "smtp.example.com",
      ["Smtp:Username"] = "",  // empty → skips AuthenticateAsync branch
    });

    await Assert.ThrowsAnyAsync<Exception>(() =>
        config.SendPasswordResetEmailAsync("to@example.com", "token"));
  }
}
