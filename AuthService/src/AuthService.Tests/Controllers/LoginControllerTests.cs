using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using AuthService.Controllers;
using AuthService.Services;
using AuthService.Models.DTOs;

public class LoginControllerTests
{
  private readonly Mock<ILoginService> _mockLoginService;
  private readonly LoginController _controller;

  public LoginControllerTests()
  {
    _mockLoginService = new Mock<ILoginService>();
    _controller = new LoginController(_mockLoginService.Object);
  }

  [Fact]
  public async Task Login_ShouldReturnOk_WithTokenAndRefreshToken_WhenCredentialsAreValid()
  {
    // Arrange
    var request = new LoginRequest { Email = "test@example.com", Password = "SecurePassword123" };
    var loginResponse = new LoginResponse { Token = "jwt-token-string", RefreshToken = "refresh-token-string" };
    _mockLoginService
        .Setup(s => s.LoginAsync(request))
        .ReturnsAsync(ServiceResult.Success(loginResponse, "Login successful."));

    // Act
    var result = await _controller.Login(request);

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var response = Assert.IsType<LoginResponse>(okResult.Value);
    Assert.Equal("jwt-token-string", response.Token);
    Assert.Equal("refresh-token-string", response.RefreshToken);
  }

  [Fact]
  public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
  {
    // Arrange
    var request = new LoginRequest { Email = "test@example.com", Password = "WrongPassword" };
    _mockLoginService
        .Setup(s => s.LoginAsync(request))
        .ReturnsAsync(ServiceResult.Failure("Invalid email or password.", 401));

    // Act
    var result = await _controller.Login(request);

    // Assert
    var statusCodeResult = Assert.IsType<ObjectResult>(result);
    Assert.Equal(401, statusCodeResult.StatusCode);
    Assert.Contains("Invalid email or password.", statusCodeResult.Value!.ToString());
  }

  [Fact]
  public async Task Login_ShouldReturnInternalServerError_WhenTokenIsNull()
  {
    // Arrange
    var request = new LoginRequest { Email = "test@example.com", Password = "SecurePassword123" };
    _mockLoginService
        .Setup(s => s.LoginAsync(request))
        .ReturnsAsync(ServiceResult.Success(null, "Login successful."));

    // Act
    var result = await _controller.Login(request);

    // Assert
    var statusCodeResult = Assert.IsType<ObjectResult>(result);
    Assert.Equal(500, statusCodeResult.StatusCode);
  }

  [Fact]
  public async Task Refresh_ShouldReturnOk_WithNewTokens_WhenRefreshTokenIsValid()
  {
    // Arrange
    var request = new RefreshRequest { RefreshToken = "valid-refresh-token" };
    var loginResponse = new LoginResponse { Token = "new-jwt-token", RefreshToken = "new-refresh-token" };
    _mockLoginService
        .Setup(s => s.RefreshAsync(request))
        .ReturnsAsync(ServiceResult.Success(loginResponse, "Token refreshed."));

    // Act
    var result = await _controller.Refresh(request);

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var response = Assert.IsType<LoginResponse>(okResult.Value);
    Assert.Equal("new-jwt-token", response.Token);
    Assert.Equal("new-refresh-token", response.RefreshToken);
  }

  [Fact]
  public async Task Refresh_ShouldReturnUnauthorized_WhenRefreshTokenIsInvalid()
  {
    // Arrange
    var request = new RefreshRequest { RefreshToken = "bad-token" };
    _mockLoginService
        .Setup(s => s.RefreshAsync(request))
        .ReturnsAsync(ServiceResult.Failure("Invalid or expired refresh token.", 401));

    // Act
    var result = await _controller.Refresh(request);

    // Assert
    var statusCodeResult = Assert.IsType<ObjectResult>(result);
    Assert.Equal(401, statusCodeResult.StatusCode);
    Assert.Contains("Invalid or expired refresh token.", statusCodeResult.Value!.ToString());
  }

  [Fact]
  public async Task Refresh_ShouldReturnInternalServerError_WhenTokenIsNull()
  {
    var request = new RefreshRequest { RefreshToken = "valid-refresh-token" };
    _mockLoginService
        .Setup(s => s.RefreshAsync(request))
        .ReturnsAsync(ServiceResult.Success(null, "Token refreshed."));

    var result = await _controller.Refresh(request);

    var statusCodeResult = Assert.IsType<ObjectResult>(result);
    Assert.Equal(500, statusCodeResult.StatusCode);
  }

  [Fact]
  public async Task Refresh_ShouldReturnInternalServerError_WhenTokenIsEmpty()
  {
    var request = new RefreshRequest { RefreshToken = "valid-refresh-token" };
    _mockLoginService
        .Setup(s => s.RefreshAsync(request))
        .ReturnsAsync(ServiceResult.Success(new LoginResponse { Token = "", RefreshToken = "rt" }, "Token refreshed."));

    var result = await _controller.Refresh(request);

    var statusCodeResult = Assert.IsType<ObjectResult>(result);
    Assert.Equal(500, statusCodeResult.StatusCode);
  }

  [Fact]
  public async Task Login_ShouldReturnInternalServerError_WhenTokenIsWhitespace()
  {
    var request = new LoginRequest { Email = "test@example.com", Password = "SecurePassword123" };
    _mockLoginService
        .Setup(s => s.LoginAsync(request))
        .ReturnsAsync(ServiceResult.Success(new LoginResponse { Token = "   ", RefreshToken = "rt" }, "Login successful."));

    var result = await _controller.Login(request);

    var statusCodeResult = Assert.IsType<ObjectResult>(result);
    Assert.Equal(500, statusCodeResult.StatusCode);
  }

  [Fact]
  public void GetCurrentUser_ShouldReturnOk_WithClaimsFromToken()
  {
    // Arrange
    var claims = new List<Claim>
    {
      new Claim("UserId", "a1b2c3d4-0000-0000-0000-000000000000"),
      new Claim(ClaimTypes.NameIdentifier, "test@example.com"),
      new Claim(ClaimTypes.Role, "Member")
    };
    _controller.ControllerContext = new ControllerContext
    {
      HttpContext = new DefaultHttpContext
      {
        User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
      }
    };

    // Act
    var result = _controller.GetCurrentUser();

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    Assert.NotNull(okResult.Value);
    var json = okResult.Value.ToString();
    Assert.Contains("a1b2c3d4-0000-0000-0000-000000000000", json);
    Assert.Contains("test@example.com", json);
    Assert.Contains("Member", json);
  }
}
