using SharedLibrary.Enums;

namespace UserManagementService.Models.DTOs;

public class TeamMemberResponse
{
  public Guid UserId { get; set; }
  public string DisplayName { get; set; } = string.Empty;
  public UserRole Role { get; set; }
}
