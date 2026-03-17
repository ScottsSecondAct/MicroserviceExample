using Moq;
using MassTransit;
using AuthService.Models;
using AuthService.Repository;
using AuthService.Services;
using SharedLibrary.Messaging.Events;

public class RegistrationServiceTests
{
  private readonly Mock<IUserRepository> _mockUserRepository;
  private readonly Mock<IPasswordService> _mockPasswordService;
  private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
  private readonly Mock<IPasswordPolicyService> _mockPasswordPolicyService;
  private readonly RegistationService _service;

  public RegistrationServiceTests()
  {
    _mockUserRepository = new Mock<IUserRepository>();
    _mockPasswordService = new Mock<IPasswordService>();
    _mockPublishEndpoint = new Mock<IPublishEndpoint>();
    _mockPasswordPolicyService = new Mock<IPasswordPolicyService>();

    // Default: policy passes
    _mockPasswordPolicyService
        .Setup(p => p.Validate(It.IsAny<string>()))
        .Returns((true, (IReadOnlyList<string>)Array.Empty<string>()));

    _service = new RegistationService(
        _mockUserRepository.Object,
        _mockPasswordService.Object,
        _mockPublishEndpoint.Object,
        _mockPasswordPolicyService.Object);
  }

  [Fact]
  public async Task RegisterUserAsync_ShouldSaveUserAndPublishEvent_WhenEmailIsNew()
  {
    // Arrange
    var email = "newuser@example.com";
    var password = "SecurePass123";

    _mockUserRepository.Setup(r => r.GetUserByEmailAsync(email)).ReturnsAsync((User?)null);
    _mockUserRepository.Setup(r => r.AddUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
    _mockPasswordService.Setup(p => p.HashPassword(password)).Returns("hashed-password");
    _mockPublishEndpoint
        .Setup(p => p.Publish(It.IsAny<UserRegistered>(), It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

    // Act
    var result = await _service.RegisterUserAsync(email, password);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(200, result.StatusCode);
    _mockUserRepository.Verify(r => r.AddUserAsync(It.Is<User>(u => u.Email == email)), Times.Once);
    _mockPublishEndpoint.Verify(
        p => p.Publish(It.Is<UserRegistered>(e => e.Email == email), It.IsAny<CancellationToken>()),
        Times.Once);
  }

  [Fact]
  public async Task RegisterUserAsync_ShouldReturnFailure_WhenEmailAlreadyExists()
  {
    // Arrange
    var email = "existing@example.com";
    var existing = new User { UserId = Guid.NewGuid(), Email = email, PasswordHash = "hash" };
    _mockUserRepository.Setup(r => r.GetUserByEmailAsync(email)).ReturnsAsync(existing);

    // Act
    var result = await _service.RegisterUserAsync(email, "AnyPassword");

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal(409, result.StatusCode);
    _mockUserRepository.Verify(r => r.AddUserAsync(It.IsAny<User>()), Times.Never);
    _mockPublishEndpoint.Verify(
        p => p.Publish(It.IsAny<UserRegistered>(), It.IsAny<CancellationToken>()),
        Times.Never);
  }

  [Fact]
  public async Task RegisterUserAsync_ShouldReturnFailure_WhenPasswordViolatesPolicy()
  {
    // Arrange
    var errors = (IReadOnlyList<string>)new[] { "Password must be at least 8 characters long." };
    _mockPasswordPolicyService
        .Setup(p => p.Validate("weak"))
        .Returns((false, errors));

    // Act
    var result = await _service.RegisterUserAsync("user@example.com", "weak");

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal(400, result.StatusCode);
    Assert.Contains("8 characters", result.Message);
    _mockUserRepository.Verify(r => r.AddUserAsync(It.IsAny<User>()), Times.Never);
  }

  [Fact]
  public async Task ValidateEmailAsync_ShouldReturnTrue_WhenEmailIsNew()
  {
    _mockUserRepository.Setup(r => r.GetUserByEmailAsync("new@example.com")).ReturnsAsync((User?)null);

    var result = await _service.ValidateEmailAsync("new@example.com");

    Assert.True(result);
  }

  [Fact]
  public async Task ValidateEmailAsync_ShouldReturnFalse_WhenEmailExists()
  {
    var existing = new User { UserId = Guid.NewGuid(), Email = "taken@example.com", PasswordHash = "hash" };
    _mockUserRepository.Setup(r => r.GetUserByEmailAsync("taken@example.com")).ReturnsAsync(existing);

    var result = await _service.ValidateEmailAsync("taken@example.com");

    Assert.False(result);
  }
}
