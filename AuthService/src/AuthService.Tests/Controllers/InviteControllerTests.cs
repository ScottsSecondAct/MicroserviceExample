using System.Reflection;
using System.Security.Claims;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AuthService.Controllers;
using AuthService.Models.DTOs;
using AuthService.Services;

public class InviteControllerTests
{
  private readonly Mock<IInviteService> _mockInviteService;
  private readonly Mock<ILogger<InviteController>> _mockLogger;
  private readonly InviteController _controller;
  private readonly Guid _adminUserId = Guid.NewGuid();

  public InviteControllerTests()
  {
    _mockInviteService = new Mock<IInviteService>();
    _mockLogger = new Mock<ILogger<InviteController>>();
    _controller = new InviteController(_mockInviteService.Object, _mockLogger.Object);

    // Set up a fake admin identity on the controller's HttpContext
    var claims = new List<Claim>
    {
      new Claim("UserId", _adminUserId.ToString()),
      new Claim(ClaimTypes.Role, "Admin")
    };
    var identity = new ClaimsIdentity(claims, "TestAuth");
    var principal = new ClaimsPrincipal(identity);
    _controller.ControllerContext = new ControllerContext
    {
      HttpContext = new DefaultHttpContext { User = principal }
    };
  }

  // ── Invite endpoint ──────────────────────────────────────────────────────────

  [Fact]
  public async Task Invite_ShouldReturnOk_WhenInviteSucceeds()
  {
    // Arrange
    var request = new InviteRequest { Email = "newuser@example.com" };
    _mockInviteService
        .Setup(s => s.CreateInviteAsync(request.Email, _adminUserId))
        .ReturnsAsync(ServiceResult.Success(null, "Invite sent successfully."));

    // Act
    var result = await _controller.Invite(request);

    // Assert
    var ok = Assert.IsType<OkObjectResult>(result);
    Assert.NotNull(ok.Value);
    Assert.Contains("Invite sent successfully.", ok.Value.ToString());
  }

  [Fact]
  public async Task Invite_ShouldReturnBadRequest_WhenEmailIsMissing()
  {
    var request = new InviteRequest { Email = "" };

    var result = await _controller.Invite(request);

    var badRequest = Assert.IsType<BadRequestObjectResult>(result);
    Assert.Contains("Email is required.", badRequest.Value!.ToString());
  }

  [Fact]
  public async Task Invite_ShouldReturnConflict_WhenEmailAlreadyRegistered()
  {
    // Arrange
    var request = new InviteRequest { Email = "existing@example.com" };
    _mockInviteService
        .Setup(s => s.CreateInviteAsync(request.Email, _adminUserId))
        .ReturnsAsync(ServiceResult.Failure("A user with this email is already registered.", 409));

    // Act
    var result = await _controller.Invite(request);

    // Assert
    var statusResult = Assert.IsType<ObjectResult>(result);
    Assert.Equal(409, statusResult.StatusCode);
  }

  [Fact]
  public async Task Invite_ShouldReturn500_WhenExceptionIsThrown()
  {
    // Arrange
    var request = new InviteRequest { Email = "user@example.com" };
    _mockInviteService
        .Setup(s => s.CreateInviteAsync(request.Email, _adminUserId))
        .ThrowsAsync(new Exception("DB error"));

    // Act
    var result = await _controller.Invite(request);

    // Assert
    var statusResult = Assert.IsType<ObjectResult>(result);
    Assert.Equal(500, statusResult.StatusCode);
  }

  [Fact]
  public void Invite_ShouldRequireAdminPolicy()
  {
    var method = typeof(InviteController).GetMethod(nameof(InviteController.Invite));
    var authorizeAttr = method!.GetCustomAttribute<AuthorizeAttribute>();

    Assert.NotNull(authorizeAttr);
    Assert.Equal("admin", authorizeAttr.Policy);
  }

  // ── AcceptInvite endpoint ────────────────────────────────────────────────────

  [Fact]
  public async Task AcceptInvite_ShouldReturnOk_WhenTokenIsValid()
  {
    // Arrange
    var request = new AcceptInviteRequest { Token = "valid-token", Password = "NewPass123" };
    _mockInviteService
        .Setup(s => s.AcceptInviteAsync(request.Token, request.Password))
        .ReturnsAsync(ServiceResult.Success(null, "Account created successfully."));

    // Act
    var result = await _controller.AcceptInvite(request);

    // Assert
    var ok = Assert.IsType<OkObjectResult>(result);
    Assert.Contains("Account created successfully.", ok.Value!.ToString());
  }

  [Fact]
  public async Task AcceptInvite_ShouldReturnBadRequest_WhenTokenOrPasswordIsMissing()
  {
    var request = new AcceptInviteRequest { Token = "", Password = "" };

    var result = await _controller.AcceptInvite(request);

    var badRequest = Assert.IsType<BadRequestObjectResult>(result);
    Assert.Contains("Token and password are required.", badRequest.Value!.ToString());
  }

  [Fact]
  public async Task AcceptInvite_ShouldReturnBadRequest_WhenTokenIsInvalidOrExpired()
  {
    // Arrange
    var request = new AcceptInviteRequest { Token = "bad-token", Password = "Pass123" };
    _mockInviteService
        .Setup(s => s.AcceptInviteAsync(request.Token, request.Password))
        .ReturnsAsync(ServiceResult.Failure("Invalid or expired invite token.", 400));

    // Act
    var result = await _controller.AcceptInvite(request);

    // Assert
    var statusResult = Assert.IsType<ObjectResult>(result);
    Assert.Equal(400, statusResult.StatusCode);
  }

  [Fact]
  public async Task AcceptInvite_ShouldReturn500_WhenExceptionIsThrown()
  {
    // Arrange
    var request = new AcceptInviteRequest { Token = "token", Password = "Pass123" };
    _mockInviteService
        .Setup(s => s.AcceptInviteAsync(request.Token, request.Password))
        .ThrowsAsync(new Exception("DB error"));

    // Act
    var result = await _controller.AcceptInvite(request);

    // Assert
    var statusResult = Assert.IsType<ObjectResult>(result);
    Assert.Equal(500, statusResult.StatusCode);
  }

  [Fact]
  public void AcceptInvite_ShouldAllowAnonymous()
  {
    var method = typeof(InviteController).GetMethod(nameof(InviteController.AcceptInvite));
    var allowAnonAttr = method!.GetCustomAttribute<AllowAnonymousAttribute>();

    Assert.NotNull(allowAnonAttr);
  }
}
