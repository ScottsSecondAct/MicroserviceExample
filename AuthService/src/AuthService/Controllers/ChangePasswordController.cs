using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AuthService.Models.DTOs;
using AuthService.Services;

namespace AuthService.Controllers;

[ApiController]
public class ChangePasswordController : ControllerBase
{
  private readonly IChangePasswordService _changePasswordService;
  private readonly ILogger<ChangePasswordController> _logger;

  public ChangePasswordController(IChangePasswordService changePasswordService, ILogger<ChangePasswordController> logger)
  {
    _changePasswordService = changePasswordService;
    _logger = logger;
  }

  // <summary>
  // Change password for the authenticated user and clear the MustChangePassword flag.
  // Returns a new JWT token with MustChangePassword set to false.
  // </summary>
  // <response code="200">Password changed, new token returned</response>
  // <response code="400">New password is required</response>
  // <response code="401">Not authenticated</response>
  // <response code="500">Internal server error</response>
  [Authorize]
  [HttpPost("api/auth/change-password")]
  public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
  {
    if (request == null || string.IsNullOrWhiteSpace(request.NewPassword))
    {
      return BadRequest(new { message = "New password is required." });
    }

    var userIdStr = User.FindFirstValue("UserId");
    if (!Guid.TryParse(userIdStr, out var userId))
    {
      return Unauthorized(new { message = "Invalid token." });
    }

    try
    {
      var result = await _changePasswordService.ChangePasswordAsync(userId, request.NewPassword);

      if (!result.IsSuccess)
      {
        return StatusCode(result.StatusCode, new { message = result.Message });
      }

      return Ok(result.Data);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "An error occurred while changing the password.");
      return StatusCode(500, new { message = "An internal server error occurred." });
    }
  }
}
