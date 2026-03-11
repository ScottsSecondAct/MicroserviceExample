using SharedLibrary.Messaging.Events;

namespace SharedLibrary.Contacts.Events;

public record ContactCreated : BaseEvent
{
  public Guid ContactId { get; init; }
  public string FirstName { get; init; } = string.Empty;
  public string LastName { get; init; } = string.Empty;
  public string Email { get; init; } = string.Empty;
  public Guid? AccountId { get; init; }
  public Guid? OwnerId { get; init; }
}
