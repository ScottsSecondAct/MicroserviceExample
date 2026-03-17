using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using AuthService.Models;
using SharedLibrary.Enums;

namespace AuthService.Services;

public class JwtTokenService : IJwtTokenService
{
  private readonly IConfiguration _configuration;

  public JwtTokenService(IConfiguration configuration)
  {
    _configuration = configuration;
  }

  public string GenerateJwtToken(User user, UserRole role)
  {
    var jwtSettings = _configuration.GetSection("JwtSettings");

    var secretKey = jwtSettings.GetValue<string>("SecretKey");
    var issuer = jwtSettings.GetValue<string>("Issuer");
    var audience = jwtSettings.GetValue<string>("Audience");

    if (string.IsNullOrEmpty(secretKey))
    {
      throw new ArgumentNullException(nameof(secretKey), "SecretKey cannot be null or empty.");
    }

    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
      new Claim(JwtRegisteredClaimNames.Sub, user.Email),
      new Claim("UserId", user.UserId.ToString()),
      new Claim(ClaimTypes.Role, role.ToString()),
      new Claim("MustChangePassword", user.MustChangePassword.ToString().ToLower()),
      new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    var token = new JwtSecurityToken(
        issuer,
        audience,
        claims,
        expires: DateTime.UtcNow.AddHours(2),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
  }
}
