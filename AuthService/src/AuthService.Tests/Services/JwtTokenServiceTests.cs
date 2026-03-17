using System;
using System.IdentityModel.Tokens.Jwt;
using AuthService.Models;
using AuthService.Services;
using Microsoft.Extensions.Configuration;
using SharedLibrary.Enums;
using Xunit;

public class JwtTokenServiceTests
{
  private readonly JwtTokenService _jwtTokenService;

  public JwtTokenServiceTests()
  {
    var inMemorySettings = new Dictionary<string, string?>
    {
      { "JwtSettings:SecretKey", "0OxuaniZJXKKmN1TD1bsolnr3rwNK9bOTIczA6Xrsik=" },
      { "JwtSettings:Issuer", "https://localhost" },
      { "JwtSettings:Audience", "YourAppUsers" }
    };

    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(inMemorySettings)
        .Build();

    _jwtTokenService = new JwtTokenService(configuration);
  }

  [Fact]
  public void GenerateJwtToken_ShouldReturnValidToken()
  {
    // Arrange
    var user = new User { UserId = Guid.NewGuid(), Email = "test@example.com" };

    // Act
    var token = _jwtTokenService.GenerateJwtToken(user, UserRole.Member);

    // Assert
    Assert.NotNull(token);
    Assert.NotEmpty(token);
  }

  [Theory]
  [InlineData(UserRole.SalesRep)]
  [InlineData(UserRole.Manager)]
  public void GenerateJwtToken_WithCrmRole_ShouldReturnValidToken(UserRole role)
  {
    var user = new User { UserId = Guid.NewGuid(), Email = "crm@example.com" };

    var token = _jwtTokenService.GenerateJwtToken(user, role);

    Assert.NotNull(token);
    Assert.NotEmpty(token);
  }

  [Fact]
  public void GenerateJwtToken_ShouldIncludeMustChangePasswordClaim_WhenTrue()
  {
    // Arrange
    var user = new User { UserId = Guid.NewGuid(), Email = "invited@example.com", MustChangePassword = true };

    // Act
    var token = _jwtTokenService.GenerateJwtToken(user, UserRole.Member);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var parsed = handler.ReadJwtToken(token);
    var claim = parsed.Claims.FirstOrDefault(c => c.Type == "MustChangePassword");
    Assert.NotNull(claim);
    Assert.Equal("true", claim.Value);
  }

  [Fact]
  public void GenerateJwtToken_ShouldIncludeMustChangePasswordClaim_WhenFalse()
  {
    // Arrange
    var user = new User { UserId = Guid.NewGuid(), Email = "user@example.com", MustChangePassword = false };

    // Act
    var token = _jwtTokenService.GenerateJwtToken(user, UserRole.Member);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var parsed = handler.ReadJwtToken(token);
    var claim = parsed.Claims.FirstOrDefault(c => c.Type == "MustChangePassword");
    Assert.NotNull(claim);
    Assert.Equal("false", claim.Value);
  }
}
