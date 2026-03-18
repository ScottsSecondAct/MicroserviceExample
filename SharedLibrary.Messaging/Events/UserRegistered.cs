namespace SharedLibrary.Messaging.Events;

public record UserRegistered : BaseEvent
{
  public Guid UserId { get; init; }
  public string Email { get; init; } = string.Empty;
  public string Username { get; init; } = string.Empty;
  public Guid? TenantId { get; init; }
}
