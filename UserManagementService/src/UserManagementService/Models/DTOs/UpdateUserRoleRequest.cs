using SharedLibrary.Enums;

namespace UserManagementService.Models.DTOs;

public class UpdateUserRoleRequest
{
  public UserRole Role { get; set; }
}
