using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SharedLibrary.Enums;
using SharedLibrary.Messaging.Events;
using UserManagementService.Consumers;
using UserManagementService.Data;
using UserManagementService.Models;
using UserManagementService.Repository;
using UserManagementService.Services;

namespace UserManagementService.Tests.Consumers;

public class UserInvitedConsumerTests
{
  private readonly Mock<IUserProfileRepository> _mockRepository;
  private readonly Mock<IAuditLogService> _mockAuditLogService;
  private readonly Mock<ILogger<UserInvitedConsumer>> _mockLogger;
  private readonly UserManagementDbContext _db;
  private readonly UserInvitedConsumer _consumer;

  private static readonly Guid TestTenantId = new("00000000-0000-0000-0000-000000000010");

  public UserInvitedConsumerTests()
  {
    _mockRepository = new Mock<IUserProfileRepository>();
    _mockAuditLogService = new Mock<IAuditLogService>();
    _mockLogger = new Mock<ILogger<UserInvitedConsumer>>();

    _db = new UserManagementDbContext(
      new DbContextOptionsBuilder<UserManagementDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    _db.Tenants.Add(new Tenant { TenantId = TestTenantId, Slug = "default", DisplayName = "Default Tenant", CreatedAt = DateTime.UtcNow });
    _db.SaveChanges();

    _consumer = new UserInvitedConsumer(_mockRepository.Object, _mockAuditLogService.Object, _db, _mockLogger.Object);
  }

  [Fact]
  public async Task Consume_ShouldCreateStubProfile_WithInvitePendingAt()
  {
    // Arrange
    var invitedUserId = Guid.NewGuid();
    var invitedByUserId = Guid.NewGuid();
    var email = "invited@example.com";
    var message = new UserInvited { InvitedUserId = invitedUserId, Email = email, InvitedByUserId = invitedByUserId };

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
  public async Task Consume_ShouldLogInviteSent_ToAuditLog()
  {
    // Arrange
    var invitedUserId = Guid.NewGuid();
    var invitedByUserId = Guid.NewGuid();
    var message = new UserInvited { InvitedUserId = invitedUserId, Email = "invited@example.com", InvitedByUserId = invitedByUserId };

    var mockContext = new Mock<ConsumeContext<UserInvited>>();
    mockContext.Setup(c => c.Message).Returns(message);

    _mockRepository.Setup(r => r.GetByIdAsync(invitedUserId)).ReturnsAsync((UserProfile?)null);
    _mockRepository.Setup(r => r.AddAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);

    // Act
    await _consumer.Consume(mockContext.Object);

    // Assert
    _mockAuditLogService.Verify(a => a.LogActionAsync(
        AuditAction.InviteSent,
        invitedByUserId,
        invitedUserId,
        null),
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

    // Assert — idempotent: no duplicate add, no audit log entry
    await act.Should().NotThrowAsync();
    _mockRepository.Verify(r => r.AddAsync(It.IsAny<UserProfile>()), Times.Never);
    _mockAuditLogService.Verify(a => a.LogActionAsync(It.IsAny<AuditAction>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
  }
}
