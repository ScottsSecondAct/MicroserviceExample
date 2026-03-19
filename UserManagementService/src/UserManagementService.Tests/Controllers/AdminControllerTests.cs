using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SharedLibrary.Enums;
using UserManagementService.Controllers;
using UserManagementService.Models.DTOs;
using UserManagementService.Services;

namespace UserManagementService.Tests.Controllers;

public class AdminControllerTests
{
  private readonly Mock<IUserProfileService> _serviceMock = new();
  private readonly Mock<ILogger<AdminController>> _loggerMock = new();
  private readonly AdminController _sut;

  public AdminControllerTests()
  {
    _sut = new AdminController(_serviceMock.Object, _loggerMock.Object);
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

  // ── GetAllUsers ───────────────────────────────────────────────────────────

  [Fact]
  public async Task GetAllUsers_ReturnsOk_WithUserList()
  {
    _serviceMock.Setup(s => s.GetAllUsersAsync())
      .ReturnsAsync(ServiceResult.Success(new List<AdminUserResponse>()));

    var result = await _sut.GetAllUsers();

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task GetAllUsers_Returns500_OnException()
  {
    _serviceMock.Setup(s => s.GetAllUsersAsync()).ThrowsAsync(new Exception());

    var result = await _sut.GetAllUsers();

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  // ── UpdateUserRole ────────────────────────────────────────────────────────

  [Fact]
  public async Task UpdateUserRole_ReturnsOk_WhenSuccessful()
  {
    var userId = Guid.NewGuid();
    var request = new UpdateUserRoleRequest { Role = UserRole.Admin };
    _serviceMock.Setup(s => s.UpdateUserRoleAsync(userId, UserRole.Admin, It.IsAny<Guid>()))
      .ReturnsAsync(ServiceResult.Success(new AdminUserResponse { UserId = userId, Role = UserRole.Admin }));

    var result = await _sut.UpdateUserRole(userId, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task UpdateUserRole_Returns404_WhenNotFound()
  {
    var userId = Guid.NewGuid();
    var request = new UpdateUserRoleRequest { Role = UserRole.Admin };
    _serviceMock.Setup(s => s.UpdateUserRoleAsync(userId, UserRole.Admin, It.IsAny<Guid>()))
      .ReturnsAsync(ServiceResult.Failure("User profile not found.", 404));

    var result = await _sut.UpdateUserRole(userId, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(404);
  }

  [Fact]
  public async Task UpdateUserRole_Returns500_OnException()
  {
    var userId = Guid.NewGuid();
    var request = new UpdateUserRoleRequest { Role = UserRole.Admin };
    _serviceMock.Setup(s => s.UpdateUserRoleAsync(userId, UserRole.Admin, It.IsAny<Guid>())).ThrowsAsync(new Exception());

    var result = await _sut.UpdateUserRole(userId, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  [Fact]
  public async Task UpdateUserRole_Returns400_WhenRoleIsUnassigned()
  {
    var userId = Guid.NewGuid();
    var request = new UpdateUserRoleRequest { Role = UserRole.Unassigned };
    _serviceMock.Setup(s => s.UpdateUserRoleAsync(userId, UserRole.Unassigned, It.IsAny<Guid>()))
      .ReturnsAsync(ServiceResult.Failure("Cannot set role to Unassigned."));

    var result = await _sut.UpdateUserRole(userId, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(400);
  }

  // ── SetUserActive ─────────────────────────────────────────────────────────

  [Fact]
  public async Task SetUserActive_ReturnsOk_WhenSuccessful()
  {
    var userId = Guid.NewGuid();
    var request = new SetUserActiveRequest { IsActive = false };
    _serviceMock.Setup(s => s.SetUserActiveAsync(userId, false, It.IsAny<Guid>()))
      .ReturnsAsync(ServiceResult.Success(new AdminUserResponse { UserId = userId, IsActive = false }));

    var result = await _sut.SetUserActive(userId, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task SetUserActive_Returns404_WhenNotFound()
  {
    var userId = Guid.NewGuid();
    var request = new SetUserActiveRequest { IsActive = false };
    _serviceMock.Setup(s => s.SetUserActiveAsync(userId, false, It.IsAny<Guid>()))
      .ReturnsAsync(ServiceResult.Failure("User profile not found.", 404));

    var result = await _sut.SetUserActive(userId, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(404);
  }

  [Fact]
  public async Task SetUserActive_Returns500_OnException()
  {
    var userId = Guid.NewGuid();
    var request = new SetUserActiveRequest { IsActive = false };
    _serviceMock.Setup(s => s.SetUserActiveAsync(userId, false, It.IsAny<Guid>())).ThrowsAsync(new Exception());

    var result = await _sut.SetUserActive(userId, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  // ── GetActorUserId fallback ───────────────────────────────────────────────

  [Fact]
  public async Task GetAllUsers_ReturnsError_WhenServiceFails()
  {
    // Covers the null Data ?? result.Message branch in GetAllUsers
    _serviceMock.Setup(s => s.GetAllUsersAsync())
      .ReturnsAsync(ServiceResult.Failure("Service error.", 503));

    var result = await _sut.GetAllUsers();

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(503);
  }

  [Fact]
  public async Task UpdateUserRole_WithInvalidUserIdClaim_PassesGuidEmptyToService()
  {
    var controller = new AdminController(_serviceMock.Object, _loggerMock.Object);
    controller.ControllerContext = new ControllerContext
    {
      HttpContext = new DefaultHttpContext
      {
        User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
          new Claim("UserId", "not-a-guid"),
        })),
      },
    };
    var userId = Guid.NewGuid();
    var request = new UpdateUserRoleRequest { Role = UserRole.Member };
    _serviceMock.Setup(s => s.UpdateUserRoleAsync(userId, UserRole.Member, Guid.Empty))
      .ReturnsAsync(ServiceResult.Success(new AdminUserResponse { UserId = userId }));

    await controller.UpdateUserRole(userId, request);

    _serviceMock.Verify(s => s.UpdateUserRoleAsync(userId, UserRole.Member, Guid.Empty), Times.Once);
  }
}
