using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AuthService.Models.DTOs;
using AuthService.Services;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoginController : ControllerBase
{
  private readonly ILoginService _loginService;

  public LoginController(ILoginService loginService)
  {
    _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
  }

  [HttpPost("login")]
  public async Task<IActionResult> Login(LoginRequest request)
  {
    var result = await _loginService.LoginAsync(request);

    if (result.IsSuccess)
    {
      if (result.Data == null || string.IsNullOrWhiteSpace(result.Data.ToString()))
      {
        return StatusCode(500, new { message = "Internal server error: Token generation failed." });
      }

      return Ok(new LoginResponse { Token = result.Data?.ToString() ?? string.Empty });
    }

    return StatusCode(result.StatusCode, new { message = result.Message });
  }

  [Authorize]
  [HttpGet("me")]
  public IActionResult GetCurrentUser()
  {
    var userId = User.FindFirstValue("UserId");
    var email = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var role = User.FindFirstValue(ClaimTypes.Role);

    return Ok(new { userId, email, role });
  }
}
