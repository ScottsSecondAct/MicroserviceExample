using UserManagementService.Models;

namespace UserManagementService.Models.DTOs;

public class AuditLogResponse
{
  public Guid Id { get; set; }
  public string Action { get; set; } = string.Empty;
  public Guid ActorUserId { get; set; }
  public Guid TargetUserId { get; set; }
  public string? Details { get; set; }
  public DateTime Timestamp { get; set; }
}
