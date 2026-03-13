using System.ComponentModel.DataAnnotations;

namespace AuthService.Models;

public class RefreshToken
{
  [Key]
  public Guid Id { get; set; }

  [Required]
  public string Token { get; set; } = string.Empty;

  [Required]
  public Guid UserId { get; set; }

  public User User { get; set; } = null!;

  public DateTime ExpiresAt { get; set; }

  public bool IsRevoked { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
