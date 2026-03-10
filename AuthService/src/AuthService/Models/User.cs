// Models/User.cs
using System.ComponentModel.DataAnnotations;
using SharedLibrary.Enums;

namespace AuthService.Models;

public class User
{
  [Key]
  public Guid UserId { get; set; }

  [Required]
  [EmailAddress]
  public string Email { get; set; } = String.Empty;

  [Required]
  public UserRole Role { get; set; } = UserRole.Unassigned;

  [Required]
  public string PasswordHash { get; set; } = String.Empty;
}
