namespace UserManagementService.Models;

public class AuditLog
{
  public Guid Id { get; set; }
  public AuditAction Action { get; set; }
  public Guid ActorUserId { get; set; }
  public Guid TargetUserId { get; set; }
  public string? Details { get; set; }
  public DateTime Timestamp { get; set; }
}
