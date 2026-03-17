using AuthService.Configuration;
using AuthService.Services;
using Microsoft.Extensions.Options;

public class PasswordPolicyServiceTests
{
  private static PasswordPolicyService CreateService(PasswordPolicy? policy = null)
  {
    policy ??= new PasswordPolicy
    {
      MinimumLength = 8,
      RequireUppercase = true,
      RequireLowercase = true,
      RequireDigit = true,
      RequireSpecialCharacter = true,
      SpecialCharacters = "!@#$%^&*()_+-=[]{}|;':\",./<>?"
    };
    return new PasswordPolicyService(Options.Create(policy));
  }

  [Fact]
  public void Validate_ValidPassword_ReturnsSuccess()
  {
    var service = CreateService();
    var (isValid, errors) = service.Validate("Secure1!");
    Assert.True(isValid);
    Assert.Empty(errors);
  }

  [Fact]
  public void Validate_TooShort_ReturnsError()
  {
    var service = CreateService();
    var (isValid, errors) = service.Validate("Ab1!");
    Assert.False(isValid);
    Assert.Contains(errors, e => e.Contains("8 characters"));
  }

  [Fact]
  public void Validate_MissingUppercase_ReturnsError()
  {
    var service = CreateService();
    var (isValid, errors) = service.Validate("secure1!");
    Assert.False(isValid);
    Assert.Contains(errors, e => e.Contains("uppercase"));
  }

  [Fact]
  public void Validate_MissingLowercase_ReturnsError()
  {
    var service = CreateService();
    var (isValid, errors) = service.Validate("SECURE1!");
    Assert.False(isValid);
    Assert.Contains(errors, e => e.Contains("lowercase"));
  }

  [Fact]
  public void Validate_MissingDigit_ReturnsError()
  {
    var service = CreateService();
    var (isValid, errors) = service.Validate("Securely!");
    Assert.False(isValid);
    Assert.Contains(errors, e => e.Contains("digit"));
  }

  [Fact]
  public void Validate_MissingSpecialCharacter_ReturnsError()
  {
    var service = CreateService();
    var (isValid, errors) = service.Validate("Secure123");
    Assert.False(isValid);
    Assert.Contains(errors, e => e.Contains("special character"));
  }

  [Fact]
  public void Validate_MultipleViolations_ReturnsAllErrors()
  {
    var service = CreateService();
    var (isValid, errors) = service.Validate("short");
    Assert.False(isValid);
    Assert.True(errors.Count > 1);
  }

  [Fact]
  public void Validate_WhenComplexityRulesDisabled_AcceptsSimplePassword()
  {
    var policy = new PasswordPolicy
    {
      MinimumLength = 4,
      RequireUppercase = false,
      RequireLowercase = false,
      RequireDigit = false,
      RequireSpecialCharacter = false
    };
    var service = CreateService(policy);
    var (isValid, errors) = service.Validate("abcd");
    Assert.True(isValid);
    Assert.Empty(errors);
  }

  [Fact]
  public void Validate_CustomMinimumLength_EnforcesCorrectLength()
  {
    var policy = new PasswordPolicy
    {
      MinimumLength = 12,
      RequireUppercase = false,
      RequireLowercase = false,
      RequireDigit = false,
      RequireSpecialCharacter = false
    };
    var service = CreateService(policy);

    var (shortValid, _) = service.Validate("ShortPass1!");
    var (longValid, _) = service.Validate("LongEnoughPass1!");

    Assert.False(shortValid);
    Assert.True(longValid);
  }
}
