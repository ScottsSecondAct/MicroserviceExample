using System.ComponentModel.DataAnnotations;

namespace AuthService.Models;

public class PasswordResetToken
{
  [Key]
  public Guid Id { get; set; }

  [Required]
  public string Token { get; set; } = string.Empty;

  public Guid UserId { get; set; }

  [Required]
  [EmailAddress]
  public string Email { get; set; } = string.Empty;

  public DateTime ExpiresAt { get; set; }

  public bool IsUsed { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
