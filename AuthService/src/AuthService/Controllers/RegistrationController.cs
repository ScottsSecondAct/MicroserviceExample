using Microsoft.AspNetCore.Mvc;
using AuthService.Models.DTOs;
using AuthService.Services;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationController : ControllerBase
{
  private readonly IRegistrationService _registrationService;

  private readonly ILogger<RegistrationController> _logger;

  public RegistrationController(IRegistrationService registrationService, ILogger<RegistrationController> logger)
  {
    _registrationService = registrationService;
    _logger = logger;
  }

  // <summary>
  // Register a new user
  // </summary>
  // <param name="request">RegisterRequest object</param>
  // <returns>RegisterResponse object</returns>
  // <response code="200">User registered successfully</response>
  // <response code="400">Email is already registered</response>
  // <response code="500">Internal server error</response>
  [HttpPost("register")]
  public async Task<IActionResult> Register(RegisterRequest request)
  {
    if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
    {
      return BadRequest(new { message = "Email and password are required." });
    }

    try
    {
      var result = await _registrationService.RegisterUserAsync(request.Email, request.Password);

      if (!result.IsSuccess)
      {
        return StatusCode(result.StatusCode, new { message = result.Message });
      }

      return Ok(new { message = result.Message });
    }
    catch (Exception ex)
    {
      // Log the exception (logging framework assumed)
      _logger.LogError(ex, "An error occurred while processing the registration.");

      return StatusCode(500, new { message = "An internal server error occurred." });
    }
  }
}
