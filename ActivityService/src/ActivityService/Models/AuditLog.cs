namespace ActivityService.Models;

public class AuditLog
{
  public Guid AuditLogId { get; set; }
  public string EntityType { get; set; } = string.Empty;
  public string EntityId { get; set; } = string.Empty;
  public string Action { get; set; } = string.Empty;
  public string? OldValues { get; set; }
  public string? NewValues { get; set; }
  public Guid? ChangedBy { get; set; }
  public DateTime ChangedAt { get; set; }
}
