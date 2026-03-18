using System.ComponentModel.DataAnnotations;

namespace AuthService.Models;

public class Tenant
{
  [Key]
  public Guid TenantId { get; set; }

  [Required]
  public string Slug { get; set; } = string.Empty;

  [Required]
  public string DisplayName { get; set; } = string.Empty;

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
