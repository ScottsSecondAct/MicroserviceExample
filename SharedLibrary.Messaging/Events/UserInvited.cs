namespace SharedLibrary.Messaging.Events;

public record UserInvited : BaseEvent
{
  public Guid InvitedUserId { get; init; }
  public string Email { get; init; } = string.Empty;
  public Guid InvitedByUserId { get; init; }
}
