using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SharedLibrary.DTOs;
using SharedLibrary.Enums;
using UserManagementService.Controllers;
using UserManagementService.Models.DTOs;
using UserManagementService.Services;

namespace UserManagementService.Tests.Controllers;

public class UsersControllerTests
{
  private readonly Mock<IUserProfileService> _serviceMock = new();
  private readonly Mock<ILogger<UsersController>> _loggerMock = new();
  private readonly UsersController _sut;

  public UsersControllerTests()
  {
    _sut = new UsersController(_serviceMock.Object, _loggerMock.Object);
  }

  // ── CreateUserProfile ─────────────────────────────────────────────────────

  [Fact]
  public async Task CreateUserProfile_ReturnsBadRequest_WhenEmailMissing()
  {
    var result = await _sut.CreateUserProfile(new CreateUserProfileRequest { UserId = Guid.NewGuid(), Email = "" });

    result.Should().BeOfType<BadRequestObjectResult>();
  }

  [Fact]
  public async Task CreateUserProfile_Returns201_OnSuccess()
  {
    var request = new CreateUserProfileRequest { UserId = Guid.NewGuid(), Email = "user@example.com" };
    _serviceMock.Setup(s => s.CreateUserProfileAsync(request))
      .ReturnsAsync(ServiceResult.Success(new CreateUserProfileResponse { UserId = request.UserId }, "Created", 201));

    var result = await _sut.CreateUserProfile(request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(201);
  }

  [Fact]
  public async Task CreateUserProfile_Returns400_WhenAlreadyExists()
  {
    var request = new CreateUserProfileRequest { UserId = Guid.NewGuid(), Email = "user@example.com" };
    _serviceMock.Setup(s => s.CreateUserProfileAsync(request))
      .ReturnsAsync(ServiceResult.Failure("User profile already exists."));

    var result = await _sut.CreateUserProfile(request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(400);
  }

  [Fact]
  public async Task CreateUserProfile_Returns500_OnException()
  {
    var request = new CreateUserProfileRequest { UserId = Guid.NewGuid(), Email = "user@example.com" };
    _serviceMock.Setup(s => s.CreateUserProfileAsync(request)).ThrowsAsync(new Exception());

    var result = await _sut.CreateUserProfile(request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
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

  // ── PatchUserStatus ───────────────────────────────────────────────────────

  [Fact]
  public async Task PatchUserStatus_ReturnsOk_WhenDeactivatingUser()
  {
    var userId = Guid.NewGuid();
    var request = new SetUserActiveRequest { IsActive = false };
    _serviceMock.Setup(s => s.SetUserActiveAsync(userId, false))
      .ReturnsAsync(ServiceResult.Success(new AdminUserResponse { UserId = userId, IsActive = false }));

    var result = await _sut.PatchUserStatus(userId, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task PatchUserStatus_ReturnsOk_WhenReactivatingUser()
  {
    var userId = Guid.NewGuid();
    var request = new SetUserActiveRequest { IsActive = true };
    _serviceMock.Setup(s => s.SetUserActiveAsync(userId, true))
      .ReturnsAsync(ServiceResult.Success(new AdminUserResponse { UserId = userId, IsActive = true }));

    var result = await _sut.PatchUserStatus(userId, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task PatchUserStatus_Returns404_WhenUserNotFound()
  {
    var userId = Guid.NewGuid();
    var request = new SetUserActiveRequest { IsActive = false };
    _serviceMock.Setup(s => s.SetUserActiveAsync(userId, false))
      .ReturnsAsync(ServiceResult.Failure("User profile not found.", 404));

    var result = await _sut.PatchUserStatus(userId, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(404);
  }

  [Fact]
  public async Task PatchUserStatus_Returns500_OnException()
  {
    var userId = Guid.NewGuid();
    var request = new SetUserActiveRequest { IsActive = false };
    _serviceMock.Setup(s => s.SetUserActiveAsync(userId, false)).ThrowsAsync(new Exception());

    var result = await _sut.PatchUserStatus(userId, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  // ── PatchUserRole ─────────────────────────────────────────────────────────

  [Fact]
  public async Task PatchUserRole_ReturnsOk_WhenSuccessful()
  {
    var userId = Guid.NewGuid();
    var request = new UpdateUserRoleRequest { Role = UserRole.Admin };
    _serviceMock.Setup(s => s.UpdateUserRoleAsync(userId, UserRole.Admin))
      .ReturnsAsync(ServiceResult.Success(new AdminUserResponse { UserId = userId, Role = UserRole.Admin }));

    var result = await _sut.PatchUserRole(userId, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task PatchUserRole_Returns400_WhenRoleIsInvalidEnumValue()
  {
    var userId = Guid.NewGuid();
    var request = new UpdateUserRoleRequest { Role = (UserRole)999 };

    var result = await _sut.PatchUserRole(userId, request);

    result.Should().BeOfType<BadRequestObjectResult>();
  }

  [Fact]
  public async Task PatchUserRole_Returns400_WhenRoleIsUnassigned()
  {
    var userId = Guid.NewGuid();
    var request = new UpdateUserRoleRequest { Role = UserRole.Unassigned };
    _serviceMock.Setup(s => s.UpdateUserRoleAsync(userId, UserRole.Unassigned))
      .ReturnsAsync(ServiceResult.Failure("Cannot set role to Unassigned. Use Member, SalesRep, Manager, or Admin."));

    var result = await _sut.PatchUserRole(userId, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(400);
  }

  [Fact]
  public async Task PatchUserRole_Returns404_WhenUserNotFound()
  {
    var userId = Guid.NewGuid();
    var request = new UpdateUserRoleRequest { Role = UserRole.Member };
    _serviceMock.Setup(s => s.UpdateUserRoleAsync(userId, UserRole.Member))
      .ReturnsAsync(ServiceResult.Failure("User profile not found.", 404));

    var result = await _sut.PatchUserRole(userId, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(404);
  }

  [Fact]
  public async Task PatchUserRole_Returns500_OnException()
  {
    var userId = Guid.NewGuid();
    var request = new UpdateUserRoleRequest { Role = UserRole.Member };
    _serviceMock.Setup(s => s.UpdateUserRoleAsync(userId, UserRole.Member)).ThrowsAsync(new Exception());

    var result = await _sut.PatchUserRole(userId, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }
}
