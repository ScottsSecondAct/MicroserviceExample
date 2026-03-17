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
}
