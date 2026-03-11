using SharedLibrary.Enums;

namespace SharedLibrary.DTOs;

public class CreateUserProfileResponse
{
  public Guid UserId { get; set; }
  public UserRole Role { get; set; } = UserRole.Unassigned;
}
