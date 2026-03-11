using SharedLibrary.Contacts.Enums;
using SharedLibrary.Messaging.Events;

namespace SharedLibrary.Contacts.Events;

public record ContactStatusChanged : BaseEvent
{
  public Guid ContactId { get; init; }
  public ContactStatus OldStatus { get; init; }
  public ContactStatus NewStatus { get; init; }
}
