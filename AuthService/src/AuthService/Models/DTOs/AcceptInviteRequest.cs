using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.DTOs;

public class AcceptInviteRequest
{
  [Required]
  public string Token { get; set; } = string.Empty;

  [Required]
  [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
  public string Password { get; set; } = string.Empty;
}
