using System;
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
}
