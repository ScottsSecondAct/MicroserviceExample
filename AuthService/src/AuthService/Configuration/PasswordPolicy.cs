namespace AuthService.Configuration;

public class PasswordPolicy
{
  public int MinimumLength { get; set; } = 8;
  public bool RequireUppercase { get; set; } = true;
  public bool RequireLowercase { get; set; } = true;
  public bool RequireDigit { get; set; } = true;
  public bool RequireSpecialCharacter { get; set; } = true;
  public string SpecialCharacters { get; set; } = "!@#$%^&*()_+-=[]{}|;':\",./<>?";
}
