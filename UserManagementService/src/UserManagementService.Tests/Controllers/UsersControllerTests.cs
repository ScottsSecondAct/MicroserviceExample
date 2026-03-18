using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using UserManagementService.Controllers;
using UserManagementService.Models.DTOs;
using UserManagementService.Services;

namespace UserManagementService.Tests.Controllers;

public class UsersControllerTests
{
  private readonly Mock<IUserProfileService> _serviceMock = new();
  private readonly Mock<IAuditLogService> _auditLogServiceMock = new();
  private readonly Mock<ILogger<UsersController>> _loggerMock = new();
  private readonly UsersController _sut;

  public UsersControllerTests()
  {
    _sut = new UsersController(_serviceMock.Object, _auditLogServiceMock.Object, _loggerMock.Object);
    _sut.ControllerContext = new ControllerContext
    {
      HttpContext = new DefaultHttpContext
      {
        User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
          new Claim("UserId", Guid.NewGuid().ToString()),
        })),
      },
    };
  }

  // ── GetUserProfile ────────────────────────────────────────────────────────

  [Fact]
  public async Task GetUserProfile_ReturnsOk_WhenFound()
  {
    var userId = Guid.NewGuid();
    _serviceMock.Setup(s => s.GetUserProfileAsync(userId))
      .ReturnsAsync(ServiceResult.Success(new { userId, email = "user@example.com" }));

    var result = await _sut.GetUserProfile(userId);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task GetUserProfile_Returns404_WhenNotFound()
  {
    var userId = Guid.NewGuid();
    _serviceMock.Setup(s => s.GetUserProfileAsync(userId))
      .ReturnsAsync(ServiceResult.Failure("User profile not found.", 404));

    var result = await _sut.GetUserProfile(userId);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(404);
  }

  [Fact]
  public async Task GetUserProfile_Returns500_OnException()
  {
    var userId = Guid.NewGuid();
    _serviceMock.Setup(s => s.GetUserProfileAsync(userId)).ThrowsAsync(new Exception());

    var result = await _sut.GetUserProfile(userId);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  // ── GetTeam ───────────────────────────────────────────────────────────────

  [Fact]
  public async Task GetTeam_ReturnsOk_WithTeamList()
  {
    _serviceMock.Setup(s => s.GetTeamAsync())
      .ReturnsAsync(ServiceResult.Success(new List<object>()));

    var result = await _sut.GetTeam();

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task GetTeam_Returns500_OnException()
  {
    _serviceMock.Setup(s => s.GetTeamAsync()).ThrowsAsync(new Exception());

    var result = await _sut.GetTeam();

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  // ── GetUserRole ───────────────────────────────────────────────────────────

  [Fact]
  public async Task GetUserRole_ReturnsOk_WhenFound()
  {
    var userId = Guid.NewGuid();
    _serviceMock.Setup(s => s.GetUserRoleAsync(userId))
      .ReturnsAsync(ServiceResult.Success(new { role = "Member" }));

    var result = await _sut.GetUserRole(userId);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task GetUserRole_Returns404_WhenNotFound()
  {
    var userId = Guid.NewGuid();
    _serviceMock.Setup(s => s.GetUserRoleAsync(userId))
      .ReturnsAsync(ServiceResult.Failure("User profile not found.", 404));

    var result = await _sut.GetUserRole(userId);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(404);
  }

  // ── ResendInvite ──────────────────────────────────────────────────────────

  [Fact]
  public async Task ResendInvite_ReturnsOk_WhenSuccessful()
  {
    var userId = Guid.NewGuid();
    _serviceMock.Setup(s => s.ResendInviteAsync(userId, It.IsAny<Guid>()))
      .ReturnsAsync(ServiceResult.Success(new { userId, email = "user@example.com" }));

    var result = await _sut.ResendInvite(userId);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task ResendInvite_Returns404_WhenUserNotFound()
  {
    var userId = Guid.NewGuid();
    _serviceMock.Setup(s => s.ResendInviteAsync(userId, It.IsAny<Guid>()))
      .ReturnsAsync(ServiceResult.Failure("User profile not found.", 404));

    var result = await _sut.ResendInvite(userId);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(404);
  }

  [Fact]
  public async Task ResendInvite_Returns400_WhenNoPendingInvite()
  {
    var userId = Guid.NewGuid();
    _serviceMock.Setup(s => s.ResendInviteAsync(userId, It.IsAny<Guid>()))
      .ReturnsAsync(ServiceResult.Failure("User does not have a pending invite."));

    var result = await _sut.ResendInvite(userId);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(400);
  }

  [Fact]
  public async Task ResendInvite_Returns500_OnException()
  {
    var userId = Guid.NewGuid();
    _serviceMock.Setup(s => s.ResendInviteAsync(userId, It.IsAny<Guid>())).ThrowsAsync(new Exception());

    var result = await _sut.ResendInvite(userId);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  // ── GetAuditLog ───────────────────────────────────────────────────────────

  [Fact]
  public async Task GetAuditLog_ReturnsOk_WithEntries()
  {
    _auditLogServiceMock.Setup(s => s.GetAuditLogsAsync())
      .ReturnsAsync(new List<AuditLogResponse>());

    var result = await _sut.GetAuditLog();

    result.Should().BeOfType<OkObjectResult>();
  }

  [Fact]
  public async Task GetAuditLog_Returns500_OnException()
  {
    _auditLogServiceMock.Setup(s => s.GetAuditLogsAsync()).ThrowsAsync(new Exception());

    var result = await _sut.GetAuditLog();

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }
}
