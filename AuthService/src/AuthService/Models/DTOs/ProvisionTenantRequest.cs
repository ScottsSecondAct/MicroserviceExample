using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.DTOs;

public class ProvisionTenantRequest
{
  [Required]
  public string Slug { get; set; } = string.Empty;

  [Required]
  public string DisplayName { get; set; } = string.Empty;

  [Required]
  [EmailAddress]
  public string AdminEmail { get; set; } = string.Empty;

  [Required]
  public string AdminPassword { get; set; } = string.Empty;

  public string AdminUsername { get; set; } = "admin";
}
