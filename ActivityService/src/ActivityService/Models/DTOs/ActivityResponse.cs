using SharedLibrary.Activities.Enums;

namespace ActivityService.Models.DTOs;

public class ActivityResponse
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
}
