using FluentAssertions;
using Moq;
using UserManagementService.Models;
using UserManagementService.Models.DTOs;
using UserManagementService.Repository;
using UserManagementService.Services;

namespace UserManagementService.Tests.Services;

public class AuditLogServiceTests
{
  private readonly Mock<IAuditLogRepository> _mockRepository;
  private readonly AuditLogService _service;

  public AuditLogServiceTests()
  {
    _mockRepository = new Mock<IAuditLogRepository>();
    _mockRepository.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
    _service = new AuditLogService(_mockRepository.Object);
  }

  [Fact]
  public async Task LogActionAsync_CreatesEntryWithCorrectFields()
  {
    var actorId = Guid.NewGuid();
    var targetId = Guid.NewGuid();
    AuditLog? captured = null;
    _mockRepository.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
      .Callback<AuditLog>(e => captured = e)
      .Returns(Task.CompletedTask);

    var before = DateTime.UtcNow;
    await _service.LogActionAsync(AuditAction.RoleChanged, actorId, targetId, "Member to Admin");

    captured.Should().NotBeNull();
    captured!.Action.Should().Be(AuditAction.RoleChanged);
    captured.ActorUserId.Should().Be(actorId);
    captured.TargetUserId.Should().Be(targetId);
    captured.Details.Should().Be("Member to Admin");
    captured.Timestamp.Should().BeOnOrAfter(before);
    captured.Id.Should().NotBeEmpty();
  }

  [Fact]
  public async Task LogActionAsync_WithNullDetails_StoresNullDetails()
  {
    var actorId = Guid.NewGuid();
    var targetId = Guid.NewGuid();
    AuditLog? captured = null;
    _mockRepository.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
      .Callback<AuditLog>(e => captured = e)
      .Returns(Task.CompletedTask);

    await _service.LogActionAsync(AuditAction.AccountDeactivated, actorId, targetId);

    captured!.Details.Should().BeNull();
  }

  [Fact]
  public async Task GetAuditLogsAsync_ReturnsMappedResponses()
  {
    var entries = new List<AuditLog>
    {
      new()
      {
        Id = Guid.NewGuid(),
        Action = AuditAction.RoleChanged,
        ActorUserId = Guid.NewGuid(),
        TargetUserId = Guid.NewGuid(),
        Details = "Member to Admin",
        Timestamp = DateTime.UtcNow
      },
      new()
      {
        Id = Guid.NewGuid(),
        Action = AuditAction.AccountDeactivated,
        ActorUserId = Guid.NewGuid(),
        TargetUserId = Guid.NewGuid(),
        Details = null,
        Timestamp = DateTime.UtcNow.AddMinutes(-5)
      }
    };
    _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(entries);

    var result = await _service.GetAuditLogsAsync();

    result.Should().HaveCount(2);
    result[0].Action.Should().Be("RoleChanged");
    result[0].Details.Should().Be("Member to Admin");
    result[1].Action.Should().Be("AccountDeactivated");
    result[1].Details.Should().BeNull();
  }

  [Fact]
  public async Task GetAuditLogsAsync_WhenNoEntries_ReturnsEmptyList()
  {
    _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<AuditLog>());

    var result = await _service.GetAuditLogsAsync();

    result.Should().BeEmpty();
  }

  [Fact]
  public async Task LogActionAsync_CallsRepositoryAddAsync()
  {
    await _service.LogActionAsync(AuditAction.InviteSent, Guid.NewGuid(), Guid.NewGuid());

    _mockRepository.Verify(r => r.AddAsync(It.IsAny<AuditLog>()), Times.Once);
  }
}
