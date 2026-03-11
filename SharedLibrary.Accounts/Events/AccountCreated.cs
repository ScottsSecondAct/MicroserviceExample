using SharedLibrary.Messaging.Events;

namespace SharedLibrary.Accounts.Events;

public record AccountCreated : BaseEvent
{
  public Guid AccountId { get; init; }
  public string Name { get; init; } = string.Empty;
}
