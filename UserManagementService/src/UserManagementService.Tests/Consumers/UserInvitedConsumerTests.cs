using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using SharedLibrary.Enums;
using SharedLibrary.Messaging.Events;
using UserManagementService.Consumers;
using UserManagementService.Models;
using UserManagementService.Repository;

namespace UserManagementService.Tests.Consumers;

public class UserInvitedConsumerTests
{
  private readonly Mock<IUserProfileRepository> _mockRepository;
  private readonly Mock<ILogger<UserInvitedConsumer>> _mockLogger;
  private readonly UserInvitedConsumer _consumer;

  public UserInvitedConsumerTests()
  {
    _mockRepository = new Mock<IUserProfileRepository>();
    _mockLogger = new Mock<ILogger<UserInvitedConsumer>>();
    _consumer = new UserInvitedConsumer(_mockRepository.Object, _mockLogger.Object);
  }

  [Fact]
  public async Task Consume_ShouldCreateStubProfile_WithInvitePendingAt()
  {
    // Arrange
    var invitedUserId = Guid.NewGuid();
    var email = "invited@example.com";
    var message = new UserInvited { InvitedUserId = invitedUserId, Email = email, InvitedByUserId = Guid.NewGuid() };

    var mockContext = new Mock<ConsumeContext<UserInvited>>();
    mockContext.Setup(c => c.Message).Returns(message);

    _mockRepository.Setup(r => r.GetByIdAsync(invitedUserId)).ReturnsAsync((UserProfile?)null);
    _mockRepository.Setup(r => r.AddAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);

    // Act
    await _consumer.Consume(mockContext.Object);

    // Assert
    _mockRepository.Verify(r => r.AddAsync(It.Is<UserProfile>(p =>
        p.UserId == invitedUserId &&
        p.Email == email &&
        p.Role == UserRole.Unassigned &&
        p.IsActive == false &&
        p.InvitePendingAt != null)),
      Times.Once);
  }

  [Fact]
  public async Task Consume_ShouldBeIdempotent_WhenStubAlreadyExists()
  {
    // Arrange
    var invitedUserId = Guid.NewGuid();
    var existing = new UserProfile { UserId = invitedUserId, Email = "invited@example.com" };
    var message = new UserInvited { InvitedUserId = invitedUserId, Email = existing.Email, InvitedByUserId = Guid.NewGuid() };

    var mockContext = new Mock<ConsumeContext<UserInvited>>();
    mockContext.Setup(c => c.Message).Returns(message);

    _mockRepository.Setup(r => r.GetByIdAsync(invitedUserId)).ReturnsAsync(existing);

    // Act
    var act = () => _consumer.Consume(mockContext.Object);

    // Assert — idempotent: no duplicate add
    await act.Should().NotThrowAsync();
    _mockRepository.Verify(r => r.AddAsync(It.IsAny<UserProfile>()), Times.Never);
  }
}
