using System.Reflection;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AuthService.Controllers;
using AuthService.Models.DTOs;
using AuthService.Services;

public class PasswordResetControllerTests
{
  private readonly Mock<IForgotPasswordService> _mockForgotPasswordService;
  private readonly Mock<ILogger<PasswordResetController>> _mockLogger;
  private readonly PasswordResetController _controller;

  public PasswordResetControllerTests()
  {
    _mockForgotPasswordService = new Mock<IForgotPasswordService>();
    _mockLogger = new Mock<ILogger<PasswordResetController>>();
    _controller = new PasswordResetController(_mockForgotPasswordService.Object, _mockLogger.Object);
  }

  // ── ForgotPassword endpoint ──────────────────────────────────────────────────

  [Fact]
  public async Task ForgotPassword_ShouldReturnOk_WhenEmailIsValid()
  {
    // Arrange
    var request = new ForgotPasswordRequest { Email = "user@example.com" };
    _mockForgotPasswordService
        .Setup(s => s.ForgotPasswordAsync(request.Email))
        .ReturnsAsync(ServiceResult.Success(null, "If that email is registered, a reset link has been sent."));

    // Act
    var result = await _controller.ForgotPassword(request);

    // Assert
    var ok = Assert.IsType<OkObjectResult>(result);
    Assert.NotNull(ok.Value);
    Assert.Contains("reset link", ok.Value.ToString());
  }

  [Fact]
  public async Task ForgotPassword_ShouldReturnBadRequest_WhenEmailIsMissing()
  {
    var request = new ForgotPasswordRequest { Email = "" };

    var result = await _controller.ForgotPassword(request);

    var badRequest = Assert.IsType<BadRequestObjectResult>(result);
    Assert.Contains("Email is required.", badRequest.Value!.ToString());
  }

  [Fact]
  public async Task ForgotPassword_ShouldReturn500_WhenExceptionIsThrown()
  {
    // Arrange
    var request = new ForgotPasswordRequest { Email = "user@example.com" };
    _mockForgotPasswordService
        .Setup(s => s.ForgotPasswordAsync(request.Email))
        .ThrowsAsync(new Exception("DB error"));

    // Act
    var result = await _controller.ForgotPassword(request);

    // Assert
    var statusResult = Assert.IsType<ObjectResult>(result);
    Assert.Equal(500, statusResult.StatusCode);
  }

  [Fact]
  public void ForgotPassword_ShouldAllowAnonymous()
  {
    var method = typeof(PasswordResetController).GetMethod(nameof(PasswordResetController.ForgotPassword));
    var allowAnonAttr = method!.GetCustomAttribute<AllowAnonymousAttribute>();

    Assert.NotNull(allowAnonAttr);
  }

  // ── ResetPassword endpoint ───────────────────────────────────────────────────

  [Fact]
  public async Task ResetPassword_ShouldReturnOk_WhenTokenIsValid()
  {
    // Arrange
    var request = new ResetPasswordRequest { Token = "valid-token", NewPassword = "NewPass123" };
    _mockForgotPasswordService
        .Setup(s => s.ResetPasswordAsync(request.Token, request.NewPassword))
        .ReturnsAsync(ServiceResult.Success(null, "Password has been reset successfully."));

    // Act
    var result = await _controller.ResetPassword(request);

    // Assert
    var ok = Assert.IsType<OkObjectResult>(result);
    Assert.Contains("Password has been reset successfully.", ok.Value!.ToString());
  }

  [Fact]
  public async Task ResetPassword_ShouldReturnBadRequest_WhenTokenOrPasswordIsMissing()
  {
    var request = new ResetPasswordRequest { Token = "", NewPassword = "" };

    var result = await _controller.ResetPassword(request);

    var badRequest = Assert.IsType<BadRequestObjectResult>(result);
    Assert.Contains("Token and new password are required.", badRequest.Value!.ToString());
  }

  [Fact]
  public async Task ResetPassword_ShouldReturnBadRequest_WhenTokenIsInvalidOrExpired()
  {
    // Arrange
    var request = new ResetPasswordRequest { Token = "bad-token", NewPassword = "NewPass123" };
    _mockForgotPasswordService
        .Setup(s => s.ResetPasswordAsync(request.Token, request.NewPassword))
        .ReturnsAsync(ServiceResult.Failure("Invalid or expired reset token.", 400));

    // Act
    var result = await _controller.ResetPassword(request);

    // Assert
    var statusResult = Assert.IsType<ObjectResult>(result);
    Assert.Equal(400, statusResult.StatusCode);
  }

  [Fact]
  public async Task ResetPassword_ShouldReturn500_WhenExceptionIsThrown()
  {
    // Arrange
    var request = new ResetPasswordRequest { Token = "token", NewPassword = "NewPass123" };
    _mockForgotPasswordService
        .Setup(s => s.ResetPasswordAsync(request.Token, request.NewPassword))
        .ThrowsAsync(new Exception("DB error"));

    // Act
    var result = await _controller.ResetPassword(request);

    // Assert
    var statusResult = Assert.IsType<ObjectResult>(result);
    Assert.Equal(500, statusResult.StatusCode);
  }

  [Fact]
  public void ResetPassword_ShouldAllowAnonymous()
  {
    var method = typeof(PasswordResetController).GetMethod(nameof(PasswordResetController.ResetPassword));
    var allowAnonAttr = method!.GetCustomAttribute<AllowAnonymousAttribute>();

    Assert.NotNull(allowAnonAttr);
  }
}
