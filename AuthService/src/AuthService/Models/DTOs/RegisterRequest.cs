using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.DTOs;

public class RegisterRequest
{
  [Required]
  [EmailAddress]
  public string Email { get; set; } = String.Empty;

  [Required]
  [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
  public string Password { get; set; } = String.Empty;
}
