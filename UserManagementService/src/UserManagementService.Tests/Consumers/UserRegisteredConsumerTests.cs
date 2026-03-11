using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using SharedLibrary.DTOs;
using SharedLibrary.Messaging.Events;
using UserManagementService.Consumers;
using UserManagementService.Services;

namespace UserManagementService.Tests.Consumers;

public class UserRegisteredConsumerTests
{
  private readonly Mock<IUserProfileService> _mockUserProfileService;
  private readonly Mock<ILogger<UserRegisteredConsumer>> _mockLogger;
  private readonly UserRegisteredConsumer _consumer;

  public UserRegisteredConsumerTests()
  {
    _mockUserProfileService = new Mock<IUserProfileService>();
    _mockLogger = new Mock<ILogger<UserRegisteredConsumer>>();
    _consumer = new UserRegisteredConsumer(_mockUserProfileService.Object, _mockLogger.Object);
  }

  [Fact]
  public async Task Consume_ShouldCreateProfile_WhenUserRegisteredEventReceived()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var email = "test@example.com";
    var message = new UserRegistered { UserId = userId, Email = email };

    var mockContext = new Mock<ConsumeContext<UserRegistered>>();
    mockContext.Setup(c => c.Message).Returns(message);

    _mockUserProfileService
        .Setup(s => s.CreateUserProfileAsync(It.Is<CreateUserProfileRequest>(r =>
            r.UserId == userId && r.Email == email)))
        .ReturnsAsync(ServiceResult.Success(null, "Profile created.", 201));

    // Act
    await _consumer.Consume(mockContext.Object);

    // Assert
    _mockUserProfileService.Verify(
        s => s.CreateUserProfileAsync(It.Is<CreateUserProfileRequest>(r =>
            r.UserId == userId && r.Email == email)),
        Times.Once);
  }

  [Fact]
  public async Task Consume_ShouldNotThrow_WhenProfileAlreadyExists()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var message = new UserRegistered { UserId = userId, Email = "existing@example.com" };

    var mockContext = new Mock<ConsumeContext<UserRegistered>>();
    mockContext.Setup(c => c.Message).Returns(message);

    _mockUserProfileService
        .Setup(s => s.CreateUserProfileAsync(It.IsAny<CreateUserProfileRequest>()))
        .ReturnsAsync(ServiceResult.Failure("User profile already exists."));

    // Act
    var act = () => _consumer.Consume(mockContext.Object);

    // Assert — idempotent: should not throw even when profile already exists
    await act.Should().NotThrowAsync();
  }
}
