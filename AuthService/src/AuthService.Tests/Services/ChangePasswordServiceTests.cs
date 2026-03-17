using Moq;
using Microsoft.Extensions.Logging;
using AuthService.Models;
using AuthService.Models.DTOs;
using AuthService.Repository;
using AuthService.Services;
using SharedLibrary.Enums;

public class ChangePasswordServiceTests
{
  private readonly Mock<IUserRepository> _mockUserRepository;
  private readonly Mock<IPasswordService> _mockPasswordService;
  private readonly Mock<IJwtTokenService> _mockJwtTokenService;
  private readonly Mock<IUserRoleClient> _mockUserRoleClient;
  private readonly Mock<ILogger<ChangePasswordService>> _mockLogger;
  private readonly ChangePasswordService _service;

  public ChangePasswordServiceTests()
  {
    _mockUserRepository = new Mock<IUserRepository>();
    _mockPasswordService = new Mock<IPasswordService>();
    _mockJwtTokenService = new Mock<IJwtTokenService>();
    _mockUserRoleClient = new Mock<IUserRoleClient>();
    _mockLogger = new Mock<ILogger<ChangePasswordService>>();

    _service = new ChangePasswordService(
        _mockUserRepository.Object,
        _mockPasswordService.Object,
        _mockJwtTokenService.Object,
        _mockUserRoleClient.Object,
        _mockLogger.Object);
  }

  [Fact]
  public async Task ChangePasswordAsync_ShouldUpdatePasswordAndClearFlag_WhenUserExists()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var newPassword = "NewSecurePass123!";
    var user = new User
    {
      UserId = userId,
      Email = "user@example.com",
      PasswordHash = "old-hash",
      MustChangePassword = true
    };

    _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
    _mockPasswordService.Setup(p => p.HashPassword(newPassword)).Returns("new-hash");
    _mockUserRepository.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
    _mockUserRoleClient.Setup(c => c.GetRoleAsync(userId))
        .ReturnsAsync(new UserRoleResponse { Role = UserRole.Member, IsActive = true });
    _mockJwtTokenService.Setup(j => j.GenerateJwtToken(It.IsAny<User>(), UserRole.Member))
        .Returns("new-jwt-token");

    // Act
    var result = await _service.ChangePasswordAsync(userId, newPassword);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(200, result.StatusCode);
    _mockUserRepository.Verify(
        r => r.UpdateUserAsync(It.Is<User>(u => u.PasswordHash == "new-hash" && !u.MustChangePassword)),
        Times.Once);
    var response = result.Data as LoginResponse;
    Assert.NotNull(response);
    Assert.Equal("new-jwt-token", response.Token);
  }

  [Fact]
  public async Task ChangePasswordAsync_ShouldReturnFailure_WhenUserNotFound()
  {
    // Arrange
    var userId = Guid.NewGuid();
    _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

    // Act
    var result = await _service.ChangePasswordAsync(userId, "NewPass123!");

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal(404, result.StatusCode);
    _mockUserRepository.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
  }

  [Fact]
  public async Task ChangePasswordAsync_ShouldReturnFailure_WhenNewPasswordIsEmpty()
  {
    // Arrange
    var userId = Guid.NewGuid();

    // Act
    var result = await _service.ChangePasswordAsync(userId, "");

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal(400, result.StatusCode);
    _mockUserRepository.Verify(r => r.GetUserByIdAsync(It.IsAny<Guid>()), Times.Never);
  }

  [Fact]
  public async Task ChangePasswordAsync_ShouldReturnFailure_WhenNewPasswordIsWhitespace()
  {
    // Arrange
    var userId = Guid.NewGuid();

    // Act
    var result = await _service.ChangePasswordAsync(userId, "   ");

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal(400, result.StatusCode);
    _mockUserRepository.Verify(r => r.GetUserByIdAsync(It.IsAny<Guid>()), Times.Never);
  }
}
