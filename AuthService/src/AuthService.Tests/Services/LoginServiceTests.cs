using Moq;
using Microsoft.AspNetCore.Http;
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
  private readonly Mock<ITenantResolver> _mockTenantResolver;
  private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
  private readonly LoginService _service;

  private static readonly Guid TestTenantId = new("00000000-0000-0000-0000-000000000010");

  public LoginServiceTests()
  {
    _mockUserRepository = new Mock<IUserRepository>();
    _mockPasswordService = new Mock<IPasswordService>();
    _mockJwtTokenService = new Mock<IJwtTokenService>();
    _mockUserRoleClient = new Mock<IUserRoleClient>();
    _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
    _mockLogger = new Mock<ILogger<LoginService>>();
    _mockTenantResolver = new Mock<ITenantResolver>();
    _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

    _mockTenantResolver.Setup(r => r.Resolve(It.IsAny<HttpContext?>())).Returns(TestTenantId);
    _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

    _service = new LoginService(
        _mockUserRepository.Object,
        _mockPasswordService.Object,
        _mockJwtTokenService.Object,
        _mockUserRoleClient.Object,
        _mockRefreshTokenRepository.Object,
        _mockLogger.Object,
        _mockTenantResolver.Object,
        _mockHttpContextAccessor.Object);
  }

  [Fact]
  public async Task LoginAsync_ShouldReturnSuccess_WithTokenAndRefreshToken_WhenCredentialsAreValid()
  {
    // Arrange
    var user = new User { UserId = Guid.NewGuid(), Email = "test@example.com", Username = "test", TenantId = TestTenantId, PasswordHash = "hashed" };
    var request = new LoginRequest { Email = "test@example.com", Password = "SecurePassword123" };

    _mockUserRepository.Setup(r => r.GetUserByEmailAsync(request.Email)).ReturnsAsync(user);
    _mockPasswordService.Setup(p => p.VerifyPassword(request.Password, user.PasswordHash)).Returns(true);
    _mockUserRoleClient.Setup(r => r.GetRoleAsync(user.UserId)).ReturnsAsync(new UserRoleResponse { UserId = user.UserId, Role = UserRole.Member, IsActive = true });
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
    var user = new User { UserId = Guid.NewGuid(), Email = "test@example.com", Username = "test", TenantId = TestTenantId, PasswordHash = "hashed" };
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
    _mockUserRoleClient.Setup(r => r.GetRoleAsync(userId)).ReturnsAsync(new UserRoleResponse { UserId = userId, Role = UserRole.Member, IsActive = true });
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

  [Fact]
  public async Task LoginAsync_ShouldReturnFailure_WhenUserIsDeactivated()
  {
    // Arrange
    var user = new User { UserId = Guid.NewGuid(), Email = "deactivated@example.com", Username = "deactivated", TenantId = TestTenantId, PasswordHash = "hashed" };
    var request = new LoginRequest { Email = "deactivated@example.com", Password = "SecurePassword123" };

    _mockUserRepository.Setup(r => r.GetUserByEmailAsync(request.Email)).ReturnsAsync(user);
    _mockPasswordService.Setup(p => p.VerifyPassword(request.Password, user.PasswordHash)).Returns(true);
    _mockUserRoleClient.Setup(r => r.GetRoleAsync(user.UserId))
      .ReturnsAsync(new UserRoleResponse { UserId = user.UserId, Role = UserRole.Member, IsActive = false });

    // Act
    var result = await _service.LoginAsync(request);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal(403, result.StatusCode);
    Assert.Contains("deactivated", result.Message, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public async Task LoginAsync_ByUsername_ShouldReturnSuccess_WhenCredentialsAreValid()
  {
    // Arrange — input has no '@', so the username path is taken
    var user = new User { UserId = Guid.NewGuid(), Email = "test@example.com", Username = "testuser", TenantId = TestTenantId, PasswordHash = "hashed" };
    var request = new LoginRequest { Email = "testuser", Password = "SecurePassword123" };

    _mockUserRepository.Setup(r => r.GetUserByUsernameAsync(TestTenantId, "testuser")).ReturnsAsync(user);
    _mockPasswordService.Setup(p => p.VerifyPassword(request.Password, user.PasswordHash)).Returns(true);
    _mockUserRoleClient.Setup(r => r.GetRoleAsync(user.UserId)).ReturnsAsync(new UserRoleResponse { UserId = user.UserId, Role = UserRole.Member, IsActive = true });
    _mockJwtTokenService.Setup(j => j.GenerateJwtToken(user, UserRole.Member)).Returns("jwt-token-string");
    _mockRefreshTokenRepository.Setup(r => r.AddAsync(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);

    // Act
    var result = await _service.LoginAsync(request);

    // Assert
    Assert.True(result.IsSuccess);
    var loginResponse = Assert.IsType<LoginResponse>(result.Data);
    Assert.Equal("jwt-token-string", loginResponse.Token);
    _mockUserRepository.Verify(r => r.GetUserByUsernameAsync(TestTenantId, "testuser"), Times.Once);
    _mockUserRepository.Verify(r => r.GetUserByEmailAsync(It.IsAny<string>()), Times.Never);
  }

  [Fact]
  public async Task LoginAsync_ByUsername_ShouldReturnFailure_WhenUsernameNotFound()
  {
    // Arrange
    var request = new LoginRequest { Email = "unknownuser", Password = "SecurePassword123" };
    _mockUserRepository.Setup(r => r.GetUserByUsernameAsync(TestTenantId, "unknownuser")).ReturnsAsync((User?)null);

    // Act
    var result = await _service.LoginAsync(request);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal(401, result.StatusCode);
    Assert.Equal("Invalid email or password.", result.Message);
  }

  [Fact]
  public async Task RefreshAsync_ShouldReturnFailure_WhenUserIsDeactivated()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var user = new User { UserId = userId, Email = "deactivated@example.com", PasswordHash = "hashed" };
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
    _mockUserRoleClient.Setup(r => r.GetRoleAsync(userId))
      .ReturnsAsync(new UserRoleResponse { UserId = userId, Role = UserRole.Member, IsActive = false });

    // Act
    var result = await _service.RefreshAsync(request);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal(403, result.StatusCode);
    Assert.Contains("deactivated", result.Message, StringComparison.OrdinalIgnoreCase);
  }
}
