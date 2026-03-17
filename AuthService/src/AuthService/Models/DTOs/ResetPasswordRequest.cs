using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.DTOs;

public class ResetPasswordRequest
{
  [Required]
  public string Token { get; set; } = string.Empty;

  [Required]
  public string NewPassword { get; set; } = string.Empty;
}
