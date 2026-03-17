using Moq;
using Microsoft.Extensions.Configuration;
using AuthService.Models;
using AuthService.Repository;
using AuthService.Services;

public class ForgotPasswordServiceTests
{
  private readonly Mock<IUserRepository> _mockUserRepository;
  private readonly Mock<IPasswordResetTokenRepository> _mockResetTokenRepository;
  private readonly Mock<IPasswordService> _mockPasswordService;
  private readonly Mock<IEmailService> _mockEmailService;
  private readonly ForgotPasswordService _service;

  public ForgotPasswordServiceTests()
  {
    _mockUserRepository = new Mock<IUserRepository>();
    _mockResetTokenRepository = new Mock<IPasswordResetTokenRepository>();
    _mockPasswordService = new Mock<IPasswordService>();
    _mockEmailService = new Mock<IEmailService>();

    _service = new ForgotPasswordService(
        _mockUserRepository.Object,
        _mockResetTokenRepository.Object,
        _mockPasswordService.Object,
        _mockEmailService.Object);
  }

  // ── ForgotPasswordAsync ──────────────────────────────────────────────────────

  [Fact]
  public async Task ForgotPasswordAsync_ShouldReturnSuccess_AndSendEmail_WhenUserExists()
  {
    // Arrange
    var email = "user@example.com";
    var user = new User { UserId = Guid.NewGuid(), Email = email, PasswordHash = "hash" };
    _mockUserRepository.Setup(r => r.GetUserByEmailAsync(email)).ReturnsAsync(user);
    _mockResetTokenRepository.Setup(r => r.AddAsync(It.IsAny<PasswordResetToken>())).Returns(Task.CompletedTask);
    _mockEmailService.Setup(e => e.SendPasswordResetEmailAsync(email, It.IsAny<string>())).Returns(Task.CompletedTask);

    // Act
    var result = await _service.ForgotPasswordAsync(email);

    // Assert
    Assert.True(result.IsSuccess);
    _mockResetTokenRepository.Verify(r => r.AddAsync(It.Is<PasswordResetToken>(t =>
        t.UserId == user.UserId &&
        t.Email == email &&
        !t.IsUsed &&
        t.ExpiresAt > DateTime.UtcNow)), Times.Once);
    _mockEmailService.Verify(e => e.SendPasswordResetEmailAsync(email, It.IsAny<string>()), Times.Once);
  }

  [Fact]
  public async Task ForgotPasswordAsync_ShouldReturnSuccess_AndNotSendEmail_WhenUserDoesNotExist()
  {
    // Arrange
    var email = "unknown@example.com";
    _mockUserRepository.Setup(r => r.GetUserByEmailAsync(email)).ReturnsAsync((User?)null);

    // Act
    var result = await _service.ForgotPasswordAsync(email);

    // Assert — still returns success to prevent email enumeration
    Assert.True(result.IsSuccess);
    _mockResetTokenRepository.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>()), Times.Never);
    _mockEmailService.Verify(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
  }

  // ── ResetPasswordAsync ───────────────────────────────────────────────────────

  [Fact]
  public async Task ResetPasswordAsync_ShouldUpdatePasswordAndMarkTokenUsed_WhenTokenIsValid()
  {
    // Arrange
    var token = "valid-token";
    var newPassword = "NewSecurePass123";
    var userId = Guid.NewGuid();
    var resetToken = new PasswordResetToken
    {
      Id = Guid.NewGuid(),
      Token = token,
      UserId = userId,
      Email = "user@example.com",
      ExpiresAt = DateTime.UtcNow.AddHours(1),
      IsUsed = false
    };
    var user = new User { UserId = userId, Email = "user@example.com", PasswordHash = "old-hash" };

    _mockResetTokenRepository.Setup(r => r.GetByTokenAsync(token)).ReturnsAsync(resetToken);
    _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
    _mockPasswordService.Setup(p => p.HashPassword(newPassword)).Returns("new-hash");
    _mockUserRepository.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
    _mockResetTokenRepository.Setup(r => r.UpdateAsync(It.IsAny<PasswordResetToken>())).Returns(Task.CompletedTask);

    // Act
    var result = await _service.ResetPasswordAsync(token, newPassword);

    // Assert
    Assert.True(result.IsSuccess);
    _mockUserRepository.Verify(r => r.UpdateUserAsync(It.Is<User>(u => u.PasswordHash == "new-hash")), Times.Once);
    _mockResetTokenRepository.Verify(r => r.UpdateAsync(It.Is<PasswordResetToken>(t => t.IsUsed)), Times.Once);
  }

  [Fact]
  public async Task ResetPasswordAsync_ShouldReturnFailure_WhenTokenNotFound()
  {
    _mockResetTokenRepository.Setup(r => r.GetByTokenAsync("bad-token")).ReturnsAsync((PasswordResetToken?)null);

    var result = await _service.ResetPasswordAsync("bad-token", "NewPass123");

    Assert.False(result.IsSuccess);
    Assert.Equal(400, result.StatusCode);
    _mockUserRepository.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
  }

  [Fact]
  public async Task ResetPasswordAsync_ShouldReturnFailure_WhenTokenAlreadyUsed()
  {
    var resetToken = new PasswordResetToken
    {
      Token = "used-token",
      UserId = Guid.NewGuid(),
      Email = "user@example.com",
      ExpiresAt = DateTime.UtcNow.AddHours(1),
      IsUsed = true
    };
    _mockResetTokenRepository.Setup(r => r.GetByTokenAsync("used-token")).ReturnsAsync(resetToken);

    var result = await _service.ResetPasswordAsync("used-token", "NewPass123");

    Assert.False(result.IsSuccess);
    Assert.Equal(400, result.StatusCode);
    Assert.Contains("already been used", result.Message);
    _mockUserRepository.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
  }

  [Fact]
  public async Task ResetPasswordAsync_ShouldReturnFailure_WhenTokenExpired()
  {
    var resetToken = new PasswordResetToken
    {
      Token = "expired-token",
      UserId = Guid.NewGuid(),
      Email = "user@example.com",
      ExpiresAt = DateTime.UtcNow.AddHours(-1),
      IsUsed = false
    };
    _mockResetTokenRepository.Setup(r => r.GetByTokenAsync("expired-token")).ReturnsAsync(resetToken);

    var result = await _service.ResetPasswordAsync("expired-token", "NewPass123");

    Assert.False(result.IsSuccess);
    Assert.Equal(400, result.StatusCode);
    Assert.Contains("expired", result.Message);
    _mockUserRepository.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
  }

  [Fact]
  public async Task ResetPasswordAsync_ShouldReturnFailure_WhenUserNotFound()
  {
    var userId = Guid.NewGuid();
    var resetToken = new PasswordResetToken
    {
      Token = "orphan-token",
      UserId = userId,
      Email = "ghost@example.com",
      ExpiresAt = DateTime.UtcNow.AddHours(1),
      IsUsed = false
    };
    _mockResetTokenRepository.Setup(r => r.GetByTokenAsync("orphan-token")).ReturnsAsync(resetToken);
    _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

    var result = await _service.ResetPasswordAsync("orphan-token", "NewPass123");

    Assert.False(result.IsSuccess);
    Assert.Equal(400, result.StatusCode);
    _mockUserRepository.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
  }
}
