using Microsoft.Extensions.Logging;
using Moq;
using UserManagementService.Services;

namespace UserManagementService.Tests.Services;

public class EmailServiceTests
{
    [Fact]
    public async Task SendInviteEmailAsync_LogsMessageAndCompletes()
    {
        var loggerMock = new Mock<ILogger<EmailService>>();
        var svc = new EmailService(loggerMock.Object);

        await svc.SendInviteEmailAsync("user@example.com", "token-123");

        loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("user@example.com")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
