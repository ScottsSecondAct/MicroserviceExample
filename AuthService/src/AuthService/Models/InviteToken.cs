using System.ComponentModel.DataAnnotations;

namespace AuthService.Models;

public class InviteToken
{
  [Key]
  public Guid Id { get; set; }

  [Required]
  public string Token { get; set; } = string.Empty;

  [Required]
  [EmailAddress]
  public string Email { get; set; } = string.Empty;

  public DateTime ExpiresAt { get; set; }

  public bool IsUsed { get; set; }

  public Guid CreatedByUserId { get; set; }

  public Guid InvitedUserId { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
