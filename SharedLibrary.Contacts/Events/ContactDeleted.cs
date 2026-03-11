using SharedLibrary.Messaging.Events;

namespace SharedLibrary.Contacts.Events;

public record ContactDeleted : BaseEvent
{
  public Guid ContactId { get; init; }
}
