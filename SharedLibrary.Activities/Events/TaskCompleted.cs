using SharedLibrary.Messaging.Events;

namespace SharedLibrary.Activities.Events;

public record TaskCompleted : BaseEvent
{
  public Guid ActivityId { get; init; }
  public string Subject { get; init; } = "";
  public Guid? OwnerId { get; init; }
  public DateTime CompletedAt { get; init; }
}
