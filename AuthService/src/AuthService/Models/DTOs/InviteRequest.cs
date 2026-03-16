using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.DTOs;

public class InviteRequest
{
  [Required]
  [EmailAddress]
  public string Email { get; set; } = string.Empty;
}
