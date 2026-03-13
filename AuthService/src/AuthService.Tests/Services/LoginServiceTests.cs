using Moq;
using Microsoft.Extensions.Logging;
using AuthService.Services;
using AuthService.Models;
using AuthService.Models.DTOs;
using AuthService.Repository;
using SharedLibrary.Enums;

public class LoginServiceTests
{
  private readonly Mock<IUserRepository> _mockUserRepository;
  private readonly Mock<IPasswordService> _mockPasswordService;
  private readonly Mock<IJwtTokenService> _mockJwtTokenService;
  private readonly Mock<IUserRoleClient> _mockUserRoleClient;
  private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
  private readonly Mock<ILogger<LoginService>> _mockLogger;
  private readonly LoginService _service;

  public LoginServiceTests()
  {
    _mockUserRepository = new Mock<IUserRepository>();
    _mockPasswordService = new Mock<IPasswordService>();
    _mockJwtTokenService = new Mock<IJwtTokenService>();
    _mockUserRoleClient = new Mock<IUserRoleClient>();
    _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
    _mockLogger = new Mock<ILogger<LoginService>>();

    _service = new LoginService(
        _mockUserRepository.Object,
        _mockPasswordService.Object,
        _mockJwtTokenService.Object,
        _mockUserRoleClient.Object,
        _mockRefreshTokenRepository.Object,
        _mockLogger.Object);
  }

  [Fact]
  public async Task LoginAsync_ShouldReturnSuccess_WithTokenAndRefreshToken_WhenCredentialsAreValid()
  {
    // Arrange
    var user = new User { UserId = Guid.NewGuid(), Email = "test@example.com", PasswordHash = "hashed" };
    var request = new LoginRequest { Email = "test@example.com", Password = "SecurePassword123" };

    _mockUserRepository.Setup(r => r.GetUserByEmailAsync(request.Email)).ReturnsAsync(user);
    _mockPasswordService.Setup(p => p.VerifyPassword(request.Password, user.PasswordHash)).Returns(true);
    _mockUserRoleClient.Setup(r => r.GetRoleAsync(user.UserId)).ReturnsAsync(UserRole.Member);
    _mockJwtTokenService.Setup(j => j.GenerateJwtToken(user, UserRole.Member)).Returns("jwt-token-string");
    _mockRefreshTokenRepository.Setup(r => r.AddAsync(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);

    // Act
    var result = await _service.LoginAsync(request);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(200, result.StatusCode);
    var loginResponse = Assert.IsType<LoginResponse>(result.Data);
    Assert.Equal("jwt-token-string", loginResponse.Token);
    Assert.NotEmpty(loginResponse.RefreshToken);
    _mockRefreshTokenRepository.Verify(r => r.AddAsync(It.Is<RefreshToken>(t => t.UserId == user.UserId)), Times.Once);
  }

  [Fact]
  public async Task LoginAsync_ShouldReturnFailure_WhenEmailIsMissing()
  {
    // Arrange
    var request = new LoginRequest { Email = "", Password = "SecurePassword123" };

    // Act
    var result = await _service.LoginAsync(request);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal(400, result.StatusCode);
  }

  [Fact]
  public async Task LoginAsync_ShouldReturnFailure_WhenUserNotFound()
  {
    // Arrange
    var request = new LoginRequest { Email = "unknown@example.com", Password = "SecurePassword123" };
    _mockUserRepository.Setup(r => r.GetUserByEmailAsync(request.Email)).ReturnsAsync((User?)null);

    // Act
    var result = await _service.LoginAsync(request);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal(401, result.StatusCode);
    Assert.Equal("Invalid email or password.", result.Message);
  }

  [Fact]
  public async Task LoginAsync_ShouldReturnFailure_WhenPasswordIsIncorrect()
  {
    // Arrange
    var user = new User { UserId = Guid.NewGuid(), Email = "test@example.com", PasswordHash = "hashed" };
    var request = new LoginRequest { Email = "test@example.com", Password = "WrongPassword" };

    _mockUserRepository.Setup(r => r.GetUserByEmailAsync(request.Email)).ReturnsAsync(user);
    _mockPasswordService.Setup(p => p.VerifyPassword(request.Password, user.PasswordHash)).Returns(false);

    // Act
    var result = await _service.LoginAsync(request);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal(401, result.StatusCode);
    Assert.Equal("Invalid email or password.", result.Message);
  }

  [Fact]
  public async Task RefreshAsync_ShouldReturnNewTokens_WhenRefreshTokenIsValid()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var user = new User { UserId = userId, Email = "test@example.com", PasswordHash = "hashed" };
    var existingToken = new RefreshToken
    {
      Id = Guid.NewGuid(),
      Token = "valid-refresh-token",
      UserId = userId,
      User = user,
      ExpiresAt = DateTime.UtcNow.AddDays(7),
      IsRevoked = false
    };
    var request = new RefreshRequest { RefreshToken = "valid-refresh-token" };

    _mockRefreshTokenRepository.Setup(r => r.GetByTokenAsync("valid-refresh-token")).ReturnsAsync(existingToken);
    _mockRefreshTokenRepository.Setup(r => r.RevokeAsync(existingToken)).Returns(Task.CompletedTask);
    _mockRefreshTokenRepository.Setup(r => r.AddAsync(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);
    _mockUserRoleClient.Setup(r => r.GetRoleAsync(userId)).ReturnsAsync(UserRole.Member);
    _mockJwtTokenService.Setup(j => j.GenerateJwtToken(user, UserRole.Member)).Returns("new-jwt-token");

    // Act
    var result = await _service.RefreshAsync(request);

    // Assert
    Assert.True(result.IsSuccess);
    var loginResponse = Assert.IsType<LoginResponse>(result.Data);
    Assert.Equal("new-jwt-token", loginResponse.Token);
    Assert.NotEmpty(loginResponse.RefreshToken);
    _mockRefreshTokenRepository.Verify(r => r.RevokeAsync(existingToken), Times.Once);
    _mockRefreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
  }

  [Fact]
  public async Task RefreshAsync_ShouldReturnFailure_WhenRefreshTokenIsMissing()
  {
    // Arrange
    var request = new RefreshRequest { RefreshToken = "" };

    // Act
    var result = await _service.RefreshAsync(request);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal(400, result.StatusCode);
  }

  [Fact]
  public async Task RefreshAsync_ShouldReturnFailure_WhenRefreshTokenNotFound()
  {
    // Arrange
    var request = new RefreshRequest { RefreshToken = "nonexistent-token" };
    _mockRefreshTokenRepository.Setup(r => r.GetByTokenAsync("nonexistent-token")).ReturnsAsync((RefreshToken?)null);

    // Act
    var result = await _service.RefreshAsync(request);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal(401, result.StatusCode);
    Assert.Equal("Invalid or expired refresh token.", result.Message);
  }

  [Fact]
  public async Task RefreshAsync_ShouldReturnFailure_WhenRefreshTokenIsRevoked()
  {
    // Arrange
    var revokedToken = new RefreshToken
    {
      Id = Guid.NewGuid(),
      Token = "revoked-token",
      UserId = Guid.NewGuid(),
      ExpiresAt = DateTime.UtcNow.AddDays(7),
      IsRevoked = true
    };
    var request = new RefreshRequest { RefreshToken = "revoked-token" };
    _mockRefreshTokenRepository.Setup(r => r.GetByTokenAsync("revoked-token")).ReturnsAsync(revokedToken);

    // Act
    var result = await _service.RefreshAsync(request);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal(401, result.StatusCode);
    Assert.Equal("Invalid or expired refresh token.", result.Message);
  }

  [Fact]
  public async Task RefreshAsync_ShouldReturnFailure_WhenRefreshTokenIsExpired()
  {
    // Arrange
    var expiredToken = new RefreshToken
    {
      Id = Guid.NewGuid(),
      Token = "expired-token",
      UserId = Guid.NewGuid(),
      ExpiresAt = DateTime.UtcNow.AddDays(-1),
      IsRevoked = false
    };
    var request = new RefreshRequest { RefreshToken = "expired-token" };
    _mockRefreshTokenRepository.Setup(r => r.GetByTokenAsync("expired-token")).ReturnsAsync(expiredToken);

    // Act
    var result = await _service.RefreshAsync(request);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal(401, result.StatusCode);
    Assert.Equal("Invalid or expired refresh token.", result.Message);
  }
}
