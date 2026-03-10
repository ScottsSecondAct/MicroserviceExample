using System.ComponentModel.DataAnnotations;
using SharedLibrary.Enums;

namespace UserManagementService.Models;

public class UserProfile
{
  [Key]
  public Guid UserId { get; set; }

  [Required]
  [EmailAddress]
  public string Email { get; set; } = string.Empty;

  [Required]
  public UserRole Role { get; set; } = UserRole.Member;

  public string DisplayName { get; set; } = string.Empty;

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
