using System.Reflection;
using Moq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using AuthService.Controllers;
using AuthService.Models.DTOs;
using AuthService.Services;

public class ChangePasswordControllerTests
{
  private readonly Mock<IChangePasswordService> _mockChangePasswordService;
  private readonly Mock<ILogger<ChangePasswordController>> _mockLogger;
  private readonly ChangePasswordController _controller;
  private readonly Guid _userId = Guid.NewGuid();

  public ChangePasswordControllerTests()
  {
    _mockChangePasswordService = new Mock<IChangePasswordService>();
    _mockLogger = new Mock<ILogger<ChangePasswordController>>();
    _controller = new ChangePasswordController(_mockChangePasswordService.Object, _mockLogger.Object);

    var claims = new List<Claim> { new Claim("UserId", _userId.ToString()) };
    _controller.ControllerContext = new ControllerContext
    {
      HttpContext = new DefaultHttpContext
      {
        User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
      }
    };
  }

  [Fact]
  public async Task ChangePassword_ShouldReturnOk_WithNewToken_WhenSuccessful()
  {
    // Arrange
    var request = new ChangePasswordRequest { NewPassword = "NewSecurePass123!" };
    var loginResponse = new LoginResponse { Token = "new-jwt-token" };
    _mockChangePasswordService
        .Setup(s => s.ChangePasswordAsync(_userId, request.NewPassword))
        .ReturnsAsync(ServiceResult.Success(loginResponse, "Password changed successfully."));

    // Act
    var result = await _controller.ChangePassword(request);

    // Assert
    var ok = Assert.IsType<OkObjectResult>(result);
    var response = Assert.IsType<LoginResponse>(ok.Value);
    Assert.Equal("new-jwt-token", response.Token);
  }

  [Fact]
  public async Task ChangePassword_ShouldReturnBadRequest_WhenNewPasswordIsEmpty()
  {
    // Arrange
    var request = new ChangePasswordRequest { NewPassword = "" };

    // Act
    var result = await _controller.ChangePassword(request);

    // Assert
    var badRequest = Assert.IsType<BadRequestObjectResult>(result);
    Assert.Contains("New password is required.", badRequest.Value!.ToString());
    _mockChangePasswordService.Verify(s => s.ChangePasswordAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
  }

  [Fact]
  public async Task ChangePassword_ShouldReturnBadRequest_WhenRequestIsNull()
  {
    // Act
    var result = await _controller.ChangePassword(null!);

    // Assert
    var badRequest = Assert.IsType<BadRequestObjectResult>(result);
    Assert.Contains("New password is required.", badRequest.Value!.ToString());
  }

  [Fact]
  public async Task ChangePassword_ShouldReturnUnauthorized_WhenUserIdClaimIsMissing()
  {
    // Arrange
    var controller = new ChangePasswordController(_mockChangePasswordService.Object, _mockLogger.Object);
    controller.ControllerContext = new ControllerContext
    {
      HttpContext = new DefaultHttpContext
      {
        User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>(), "Test"))
      }
    };
    var request = new ChangePasswordRequest { NewPassword = "NewPass123!" };

    // Act
    var result = await controller.ChangePassword(request);

    // Assert
    var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
    Assert.Contains("Invalid token.", unauthorized.Value!.ToString());
  }

  [Fact]
  public async Task ChangePassword_ShouldReturn404_WhenUserNotFound()
  {
    // Arrange
    var request = new ChangePasswordRequest { NewPassword = "NewPass123!" };
    _mockChangePasswordService
        .Setup(s => s.ChangePasswordAsync(_userId, request.NewPassword))
        .ReturnsAsync(ServiceResult.Failure("User not found.", 404));

    // Act
    var result = await _controller.ChangePassword(request);

    // Assert
    var statusResult = Assert.IsType<ObjectResult>(result);
    Assert.Equal(404, statusResult.StatusCode);
  }

  [Fact]
  public async Task ChangePassword_ShouldReturn500_WhenExceptionIsThrown()
  {
    // Arrange
    var request = new ChangePasswordRequest { NewPassword = "NewPass123!" };
    _mockChangePasswordService
        .Setup(s => s.ChangePasswordAsync(_userId, request.NewPassword))
        .ThrowsAsync(new Exception("DB error"));

    // Act
    var result = await _controller.ChangePassword(request);

    // Assert
    var statusResult = Assert.IsType<ObjectResult>(result);
    Assert.Equal(500, statusResult.StatusCode);
  }

  [Fact]
  public void ChangePassword_ShouldRequireAuthorization()
  {
    var method = typeof(ChangePasswordController).GetMethod(nameof(ChangePasswordController.ChangePassword));
    var authorizeAttr = method!.GetCustomAttribute<AuthorizeAttribute>();

    Assert.NotNull(authorizeAttr);
  }
}
