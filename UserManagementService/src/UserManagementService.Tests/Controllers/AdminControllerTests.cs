using FluentAssertions;
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
    _serviceMock.Setup(s => s.UpdateUserRoleAsync(userId, UserRole.Admin))
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
    _serviceMock.Setup(s => s.UpdateUserRoleAsync(userId, UserRole.Admin))
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
    _serviceMock.Setup(s => s.UpdateUserRoleAsync(userId, UserRole.Admin)).ThrowsAsync(new Exception());

    var result = await _sut.UpdateUserRole(userId, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  // ── SetUserActive ─────────────────────────────────────────────────────────

  [Fact]
  public async Task SetUserActive_ReturnsOk_WhenSuccessful()
  {
    var userId = Guid.NewGuid();
    var request = new SetUserActiveRequest { IsActive = false };
    _serviceMock.Setup(s => s.SetUserActiveAsync(userId, false))
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
    _serviceMock.Setup(s => s.SetUserActiveAsync(userId, false))
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
    _serviceMock.Setup(s => s.SetUserActiveAsync(userId, false)).ThrowsAsync(new Exception());

    var result = await _sut.SetUserActive(userId, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }
}
