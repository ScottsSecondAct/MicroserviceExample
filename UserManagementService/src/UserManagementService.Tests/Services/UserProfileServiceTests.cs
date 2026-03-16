using FluentAssertions;
using Moq;
using SharedLibrary.DTOs;
using SharedLibrary.Enums;
using UserManagementService.Models;
using UserManagementService.Models.DTOs;
using UserManagementService.Repository;
using UserManagementService.Services;

namespace UserManagementService.Tests.Services;

public class UserProfileServiceTests
{
  private readonly Mock<IUserProfileRepository> _mockRepository;
  private readonly UserProfileService _service;

  public UserProfileServiceTests()
  {
    _mockRepository = new Mock<IUserProfileRepository>();
    _service = new UserProfileService(_mockRepository.Object);
  }

  [Fact]
  public async Task CreateUserProfileAsync_WithValidRequest_ReturnsSuccess()
  {
    var request = new CreateUserProfileRequest
    {
      UserId = Guid.NewGuid(),
      Email = "test@example.com"
    };

    _mockRepository.Setup(r => r.GetByIdAsync(request.UserId)).ReturnsAsync((UserProfile?)null);
    _mockRepository.Setup(r => r.AddAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);

    var result = await _service.CreateUserProfileAsync(request);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(201);
    var response = result.Data as CreateUserProfileResponse;
    response.Should().NotBeNull();
    response!.UserId.Should().Be(request.UserId);
    response.Role.Should().Be(UserRole.Member);
  }

  [Fact]
  public async Task UpdateUserRoleAsync_WithUnassignedRole_ReturnsFailure()
  {
    var userId = Guid.NewGuid();

    var result = await _service.UpdateUserRoleAsync(userId, UserRole.Unassigned);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(400);
  }

  [Fact]
  public async Task UpdateUserRoleAsync_PromotesUser_ReturnsSuccess()
  {
    var userId = Guid.NewGuid();
    var profile = new UserProfile { UserId = userId, Email = "user@example.com", Role = UserRole.Unassigned };

    _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(profile);
    _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);

    var result = await _service.UpdateUserRoleAsync(userId, UserRole.Member);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task CreateUserProfileAsync_WhenProfileAlreadyExists_ReturnsFailure()
  {
    var userId = Guid.NewGuid();
    var request = new CreateUserProfileRequest { UserId = userId, Email = "test@example.com" };
    var existing = new UserProfile { UserId = userId, Email = "test@example.com" };

    _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(existing);

    var result = await _service.CreateUserProfileAsync(request);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(400);
  }

  [Fact]
  public async Task GetUserProfileAsync_WithValidId_ReturnsProfile()
  {
    var userId = Guid.NewGuid();
    var profile = new UserProfile { UserId = userId, Email = "test@example.com", Role = UserRole.Member };

    _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(profile);

    var result = await _service.GetUserProfileAsync(userId);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task GetUserProfileAsync_WhenNotFound_ReturnsFailure()
  {
    var userId = Guid.NewGuid();
    _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((UserProfile?)null);

    var result = await _service.GetUserProfileAsync(userId);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(404);
  }

  [Fact]
  public async Task GetAllUsersAsync_ReturnsAllProfiles()
  {
    var profiles = new List<UserProfile>
    {
      new() { UserId = Guid.NewGuid(), Email = "a@test.com", DisplayName = "A", Role = UserRole.Member, IsActive = true },
      new() { UserId = Guid.NewGuid(), Email = "b@test.com", DisplayName = "B", Role = UserRole.Admin, IsActive = false },
    };
    _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(profiles);

    var result = await _service.GetAllUsersAsync();

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(200);
    var users = result.Data as List<AdminUserResponse>;
    users.Should().HaveCount(2);
  }

  [Fact]
  public async Task SetUserActiveAsync_WhenUserFound_TogglesStatus()
  {
    var userId = Guid.NewGuid();
    var profile = new UserProfile { UserId = userId, Email = "user@test.com", Role = UserRole.Member, IsActive = true };
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
