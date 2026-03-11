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
  private readonly Mock<ILogger<LoginService>> _mockLogger;
  private readonly LoginService _service;

  public LoginServiceTests()
  {
    _mockUserRepository = new Mock<IUserRepository>();
    _mockPasswordService = new Mock<IPasswordService>();
    _mockJwtTokenService = new Mock<IJwtTokenService>();
    _mockUserRoleClient = new Mock<IUserRoleClient>();
    _mockLogger = new Mock<ILogger<LoginService>>();

    _service = new LoginService(
        _mockUserRepository.Object,
        _mockPasswordService.Object,
        _mockJwtTokenService.Object,
        _mockUserRoleClient.Object,
        _mockLogger.Object);
  }

  [Fact]
  public async Task LoginAsync_ShouldReturnSuccess_WithToken_WhenCredentialsAreValid()
  {
    // Arrange
    var user = new User { UserId = Guid.NewGuid(), Email = "test@example.com", PasswordHash = "hashed" };
    var request = new LoginRequest { Email = "test@example.com", Password = "SecurePassword123" };

    _mockUserRepository.Setup(r => r.GetUserByEmailAsync(request.Email)).ReturnsAsync(user);
    _mockPasswordService.Setup(p => p.VerifyPassword(request.Password, user.PasswordHash)).Returns(true);
    _mockUserRoleClient.Setup(r => r.GetRoleAsync(user.UserId)).ReturnsAsync(UserRole.Member);
    _mockJwtTokenService.Setup(j => j.GenerateJwtToken(user, UserRole.Member)).Returns("jwt-token-string");

    // Act
    var result = await _service.LoginAsync(request);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(200, result.StatusCode);
    Assert.Equal("jwt-token-string", result.Data);
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
}
