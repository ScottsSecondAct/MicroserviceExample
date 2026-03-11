using SharedLibrary.Activities.Enums;

namespace ActivityService.Models.DTOs;

public class UpdateActivityRequest
{
  public ActivityType? Type { get; set; }
  public string? Subject { get; set; }
  public string? Notes { get; set; }
  public Guid? ContactId { get; set; }
  public Guid? DealId { get; set; }
  public Guid? AccountId { get; set; }
  public Guid? OwnerId { get; set; }
  public DateTime? ScheduledAt { get; set; }
  public DateTime? CompletedAt { get; set; }
}
