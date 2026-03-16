using SharedLibrary.Enums;

namespace UserManagementService.Models.DTOs;

public class UserRoleResponse
{
  public Guid UserId { get; set; }
  public UserRole Role { get; set; }
  public bool IsActive { get; set; }
}
