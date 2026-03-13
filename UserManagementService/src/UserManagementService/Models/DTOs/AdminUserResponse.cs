using SharedLibrary.Enums;

namespace UserManagementService.Models.DTOs;

public class AdminUserResponse
{
  public Guid UserId { get; set; }
  public string Email { get; set; } = string.Empty;
  public string DisplayName { get; set; } = string.Empty;
  public UserRole Role { get; set; }
  public bool IsActive { get; set; }
  public DateTime CreatedAt { get; set; }
}
