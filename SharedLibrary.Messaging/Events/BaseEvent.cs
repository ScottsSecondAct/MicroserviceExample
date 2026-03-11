namespace SharedLibrary.Messaging.Events;

public abstract record BaseEvent
{
  public Guid CorrelationId { get; init; } = Guid.NewGuid();
  public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
  public string EventType => GetType().Name;
}
