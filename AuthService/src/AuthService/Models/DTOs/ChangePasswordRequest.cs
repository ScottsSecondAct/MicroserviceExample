namespace AuthService.Models.DTOs;

public class ChangePasswordRequest
{
  public string NewPassword { get; set; } = string.Empty;
}
