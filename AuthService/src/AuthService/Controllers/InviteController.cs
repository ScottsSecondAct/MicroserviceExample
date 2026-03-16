using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthService.Models.DTOs;
using AuthService.Services;

namespace AuthService.Controllers;

[ApiController]
public class InviteController : ControllerBase
{
  private readonly IInviteService _inviteService;
  private readonly ILogger<InviteController> _logger;

  public InviteController(IInviteService inviteService, ILogger<InviteController> logger)
  {
    _inviteService = inviteService;
    _logger = logger;
  }

  // <summary>
  // Invite a new user by email (admin only). Generates a time-limited invite token
  // and sends an invite email to the specified address.
  // </summary>
  // <response code="200">Invite sent successfully</response>
  // <response code="400">Email is required</response>
  // <response code="401">Unauthorized — valid JWT required</response>
  // <response code="403">Forbidden — Admin role required</response>
  // <response code="409">A user with this email is already registered</response>
  // <response code="500">Internal server error</response>
  [Authorize(Policy = "admin")]
  [HttpPost("api/users/invite")]
  public async Task<IActionResult> Invite([FromBody] InviteRequest request)
  {
    if (request == null || string.IsNullOrEmpty(request.Email))
    {
      return BadRequest(new { message = "Email is required." });
    }

    var adminUserIdClaim = User.FindFirstValue("UserId");
    if (!Guid.TryParse(adminUserIdClaim, out var adminUserId))
    {
      return Unauthorized(new { message = "Invalid token claims." });
    }

    try
    {
      var result = await _inviteService.CreateInviteAsync(request.Email, adminUserId);

      if (!result.IsSuccess)
      {
        return StatusCode(result.StatusCode, new { message = result.Message });
      }

      return Ok(new { message = result.Message });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "An error occurred while processing the invite.");
      return StatusCode(500, new { message = "An internal server error occurred." });
    }
  }

  // <summary>
  // Accept an invite and set a password to complete account creation.
  // </summary>
  // <response code="200">Account created successfully</response>
  // <response code="400">Invalid or expired token, or token already used</response>
  // <response code="409">Email already registered</response>
  // <response code="500">Internal server error</response>
  [AllowAnonymous]
  [HttpPost("api/registration/accept-invite")]
  public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteRequest request)
  {
    if (request == null || string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.Password))
    {
      return BadRequest(new { message = "Token and password are required." });
    }

    try
    {
      var result = await _inviteService.AcceptInviteAsync(request.Token, request.Password);

      if (!result.IsSuccess)
      {
        return StatusCode(result.StatusCode, new { message = result.Message });
      }

      return Ok(new { message = result.Message });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "An error occurred while accepting the invite.");
      return StatusCode(500, new { message = "An internal server error occurred." });
    }
  }
}
