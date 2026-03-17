using UserManagementService.Models;
using UserManagementService.Models.DTOs;

namespace UserManagementService.Services;

public interface IAuditLogService
{
  Task LogActionAsync(AuditAction action, Guid actorUserId, Guid targetUserId, string? details = null);
  Task<List<AuditLogResponse>> GetAuditLogsAsync();
}
