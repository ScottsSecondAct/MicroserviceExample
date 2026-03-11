using SharedLibrary.Activities.Enums;
using SharedLibrary.Messaging.Events;

namespace SharedLibrary.Activities.Events;

public record ActivityLogged : BaseEvent
{
  public Guid ActivityId { get; init; }
  public ActivityType Type { get; init; }
  public string Subject { get; init; } = "";
  public Guid? ContactId { get; init; }
  public Guid? DealId { get; init; }
  public Guid? AccountId { get; init; }
  public Guid? OwnerId { get; init; }
}
