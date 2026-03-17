using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthService.Models.DTOs;
using AuthService.Services;

namespace AuthService.Controllers;

[ApiController]
public class PasswordResetController : ControllerBase
{
  private readonly IForgotPasswordService _forgotPasswordService;
  private readonly ILogger<PasswordResetController> _logger;

  public PasswordResetController(IForgotPasswordService forgotPasswordService, ILogger<PasswordResetController> logger)
  {
    _forgotPasswordService = forgotPasswordService;
    _logger = logger;
  }

  // <summary>
  // Request a password reset link. Always returns 200 to prevent email enumeration.
  // </summary>
  // <response code="200">Reset link sent if email is registered</response>
  // <response code="400">Email is required or invalid</response>
  // <response code="500">Internal server error</response>
  [AllowAnonymous]
  [HttpPost("api/auth/forgot-password")]
  public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
  {
    if (request == null || string.IsNullOrEmpty(request.Email))
    {
      return BadRequest(new { message = "Email is required." });
    }

    try
    {
      var result = await _forgotPasswordService.ForgotPasswordAsync(request.Email);
      return Ok(new { message = result.Message });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "An error occurred while processing the forgot password request.");
      return StatusCode(500, new { message = "An internal server error occurred." });
    }
  }

  // <summary>
  // Reset a password using a valid reset token.
  // </summary>
  // <response code="200">Password reset successfully</response>
  // <response code="400">Invalid, expired, or already-used token</response>
  // <response code="500">Internal server error</response>
  [AllowAnonymous]
  [HttpPost("api/auth/reset-password")]
  public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
  {
    if (request == null || string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.NewPassword))
    {
      return BadRequest(new { message = "Token and new password are required." });
    }

    try
    {
      var result = await _forgotPasswordService.ResetPasswordAsync(request.Token, request.NewPassword);

      if (!result.IsSuccess)
      {
        return StatusCode(result.StatusCode, new { message = result.Message });
      }

      return Ok(new { message = result.Message });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "An error occurred while processing the password reset.");
      return StatusCode(500, new { message = "An internal server error occurred." });
    }
  }
}
