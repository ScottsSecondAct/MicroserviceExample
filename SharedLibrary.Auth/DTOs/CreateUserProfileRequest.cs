namespace SharedLibrary.DTOs;

public class CreateUserProfileRequest
{
  public Guid UserId { get; set; }
  public string Email { get; set; } = string.Empty;
}
