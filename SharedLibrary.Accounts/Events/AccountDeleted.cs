using SharedLibrary.Messaging.Events;

namespace SharedLibrary.Accounts.Events;

public record AccountDeleted : BaseEvent
{
  public Guid AccountId { get; init; }
}
