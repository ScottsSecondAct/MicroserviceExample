using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.DTOs;

public class LoginRequest
{
  [Required]
  public string Email { get; set; } = String.Empty;  // accepts email or username

  [Required]
  [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
  public string Password { get; set; } = String.Empty;
}
