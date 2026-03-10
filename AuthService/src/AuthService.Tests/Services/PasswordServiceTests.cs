using Xunit;
using AuthService.Services;
using System;

public class PasswordServiceTests
{
  private readonly PasswordService _passwordService;

  public PasswordServiceTests()
  {
    _passwordService = new PasswordService();
  }

  [Fact]
  public void HashPassword_ShouldReturnNonNullString()
  {
    // Arrange
    string password = "securepassword";

    // Act
    string hashedPassword = _passwordService.HashPassword(password);

    // Assert
    Assert.NotNull(hashedPassword);
  }

  [Fact]
  public void VerifyPassword_ValidPassword_ShouldReturnTrue()
  {
    // Arrange
    string password = "securepassword";
    string hashedPassword = _passwordService.HashPassword(password);

    // Act
    bool result = _passwordService.VerifyPassword(password, hashedPassword);

    // Assert
    Assert.True(result);
  }

  [Fact]
  public void VerifyPassword_InvalidPassword_ShouldReturnFalse()
  {
    // Arrange
    string password = "securepassword";
    string hashedPassword = _passwordService.HashPassword(password);
    string wrongPassword = "wrongpassword";

    // Act
    bool result = _passwordService.VerifyPassword(wrongPassword, hashedPassword);

    // Assert
    Assert.False(result);
  }

  [Fact]
  public void VerifyPassword_InvalidHash_ShouldThrowFormatException()
  {
    // Arrange
    string password = "securepassword";
    string invalidHash = "invalid-base64";

    // Act & Assert
    Assert.Throws<FormatException>(() => _passwordService.VerifyPassword(password, invalidHash));
  }
}
