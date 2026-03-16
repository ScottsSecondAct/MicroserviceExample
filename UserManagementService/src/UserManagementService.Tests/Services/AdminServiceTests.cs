using FluentAssertions;
using Moq;
using SharedLibrary.Enums;
using UserManagementService.Models;
using UserManagementService.Models.DTOs;
using UserManagementService.Repository;
using UserManagementService.Services;

namespace UserManagementService.Tests.Services;

public class AdminServiceTests
{
  private readonly Mock<IUserProfileRepository> _mockRepository;
  private readonly Mock<IEmailService> _mockEmailService;
  private readonly UserProfileService _service;

  public AdminServiceTests()
  {
    _mockRepository = new Mock<IUserProfileRepository>();
    _mockEmailService = new Mock<IEmailService>();
    _mockEmailService.Setup(e => e.SendInviteEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
      .Returns(Task.CompletedTask);
    _service = new UserProfileService(_mockRepository.Object, _mockEmailService.Object);
  }

  // ── GetAllUsersAsync ──────────────────────────────────────────────────────

  [Fact]
  public async Task GetAllUsersAsync_ReturnsAllUsers()
  {
    var profiles = new List<UserProfile>
    {
      new() { UserId = Guid.NewGuid(), Email = "a@example.com", DisplayName = "Alice", Role = UserRole.Admin, IsActive = true },
      new() { UserId = Guid.NewGuid(), Email = "b@example.com", DisplayName = "Bob", Role = UserRole.Member, IsActive = false },
    };
    _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(profiles);

    var result = await _service.GetAllUsersAsync();

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(200);
    var users = result.Data as List<AdminUserResponse>;
    users.Should().NotBeNull();
    users!.Count.Should().Be(2);
    users[0].Email.Should().Be("a@example.com");
    users[1].IsActive.Should().BeFalse();
  }

  // ── UpdateUserRoleAsync ───────────────────────────────────────────────────

  [Fact]
  public async Task UpdateUserRoleAsync_WhenUserExists_UpdatesRole()
  {
    var userId = Guid.NewGuid();
    var profile = new UserProfile { UserId = userId, Email = "user@example.com", Role = UserRole.Member, IsActive = true };
    _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(profile);
    _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);

    var result = await _service.UpdateUserRoleAsync(userId, UserRole.Admin);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(200);
    var response = result.Data as AdminUserResponse;
    response!.Role.Should().Be(UserRole.Admin);
  }

  [Fact]
  public async Task UpdateUserRoleAsync_WhenUserNotFound_ReturnsFailure()
  {
    var userId = Guid.NewGuid();
    _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((UserProfile?)null);

    var result = await _service.UpdateUserRoleAsync(userId, UserRole.Admin);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(404);
  }

  [Fact]
  public async Task UpdateUserRoleAsync_WhenAssigningSalesRep_UpdatesRole()
  {
    var userId = Guid.NewGuid();
    var profile = new UserProfile { UserId = userId, Email = "user@example.com", Role = UserRole.Member, IsActive = true };
    _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(profile);
    _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);

    var result = await _service.UpdateUserRoleAsync(userId, UserRole.SalesRep);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(200);
    var response = result.Data as AdminUserResponse;
    response!.Role.Should().Be(UserRole.SalesRep);
  }

  [Fact]
  public async Task UpdateUserRoleAsync_WhenAssigningManager_UpdatesRole()
  {
    var userId = Guid.NewGuid();
    var profile = new UserProfile { UserId = userId, Email = "user@example.com", Role = UserRole.SalesRep, IsActive = true };
    _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(profile);
    _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);

    var result = await _service.UpdateUserRoleAsync(userId, UserRole.Manager);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(200);
    var response = result.Data as AdminUserResponse;
    response!.Role.Should().Be(UserRole.Manager);
  }

  // ── SetUserActiveAsync ────────────────────────────────────────────────────

  [Fact]
  public async Task SetUserActiveAsync_WhenUserExists_UpdatesActiveStatus()
  {
    var userId = Guid.NewGuid();
    var profile = new UserProfile { UserId = userId, Email = "user@example.com", Role = UserRole.Member, IsActive = true };
    _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(profile);
    _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);

    var result = await _service.SetUserActiveAsync(userId, false);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(200);
    var response = result.Data as AdminUserResponse;
    response!.IsActive.Should().BeFalse();
  }

  [Fact]
  public async Task SetUserActiveAsync_WhenUserNotFound_ReturnsFailure()
  {
    var userId = Guid.NewGuid();
    _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((UserProfile?)null);

    var result = await _service.SetUserActiveAsync(userId, false);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(404);
  }
}
