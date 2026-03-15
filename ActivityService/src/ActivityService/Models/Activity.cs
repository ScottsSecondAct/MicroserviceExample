using SharedLibrary.Activities.Enums;

namespace ActivityService.Models;

public class Activity
{
  public Guid ActivityId { get; set; }
  public ActivityType Type { get; set; }
  public string Subject { get; set; } = string.Empty;
  public string? Notes { get; set; }
  public Guid? ContactId { get; set; }
  public Guid? DealId { get; set; }
  public Guid? AccountId { get; set; }
  public Guid? OwnerId { get; set; }
  public DateTime? ScheduledAt { get; set; }
  public DateTime? CompletedAt { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
  public bool IsDeleted { get; set; }
  public DateTime? DeletedAt { get; set; }
  public Guid? DeletedBy { get; set; }
}
