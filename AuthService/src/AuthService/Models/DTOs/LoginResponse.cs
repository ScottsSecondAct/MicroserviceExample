namespace AuthService.Models.DTOs;

public class LoginResponse
{
  public string Token { get; set; } = string.Empty;
  public string RefreshToken { get; set; } = string.Empty;
  public string UserId { get; set; } = string.Empty;
}
