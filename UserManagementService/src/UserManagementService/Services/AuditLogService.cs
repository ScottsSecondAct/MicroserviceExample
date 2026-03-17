using UserManagementService.Models;
using UserManagementService.Models.DTOs;
using UserManagementService.Repository;

namespace UserManagementService.Services;

public class AuditLogService : IAuditLogService
{
  private readonly IAuditLogRepository _repository;

  public AuditLogService(IAuditLogRepository repository)
  {
    _repository = repository;
  }

  public async Task LogActionAsync(AuditAction action, Guid actorUserId, Guid targetUserId, string? details = null)
  {
    var entry = new AuditLog
    {
      Id = Guid.NewGuid(),
      Action = action,
      ActorUserId = actorUserId,
      TargetUserId = targetUserId,
      Details = details,
      Timestamp = DateTime.UtcNow
    };

    await _repository.AddAsync(entry);
  }

  public async Task<List<AuditLogResponse>> GetAuditLogsAsync()
  {
    var entries = await _repository.GetAllAsync();
    return entries.Select(e => new AuditLogResponse
    {
      Id = e.Id,
      Action = e.Action.ToString(),
      ActorUserId = e.ActorUserId,
      TargetUserId = e.TargetUserId,
      Details = e.Details,
      Timestamp = e.Timestamp
    }).ToList();
  }
}
