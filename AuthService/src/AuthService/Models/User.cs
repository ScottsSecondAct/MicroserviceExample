using System.ComponentModel.DataAnnotations;

namespace AuthService.Models;

public class User
{
  [Key]
  public Guid UserId { get; set; }

  [Required]
  [EmailAddress]
  public string Email { get; set; } = string.Empty;

  [Required]
  public string PasswordHash { get; set; } = string.Empty;

  public bool MustChangePassword { get; set; } = false;

  public Guid TenantId { get; set; }
  public Tenant? Tenant { get; set; }

  public string Username { get; set; } = string.Empty;
}
