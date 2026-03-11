using Microsoft.AspNetCore.Mvc;
using SharedLibrary.DTOs;
using UserManagementService.Services;

namespace UserManagementService.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
  private readonly IUserProfileService _userProfileService;
  private readonly ILogger<UsersController> _logger;

  public UsersController(IUserProfileService userProfileService, ILogger<UsersController> logger)
  {
    _userProfileService = userProfileService;
    _logger = logger;
  }

  [HttpPost]
  public async Task<IActionResult> CreateUserProfile([FromBody] CreateUserProfileRequest request)
  {
    if (string.IsNullOrEmpty(request.Email))
      return BadRequest("Email is required.");

    try
    {
      var result = await _userProfileService.CreateUserProfileAsync(request);
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error creating user profile");
      return StatusCode(500, "An error occurred while creating the user profile.");
    }
  }

  [HttpGet("{userId:guid}")]
  public async Task<IActionResult> GetUserProfile(Guid userId)
  {
    try
    {
      var result = await _userProfileService.GetUserProfileAsync(userId);
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving user profile");
      return StatusCode(500, "An error occurred while retrieving the user profile.");
    }
  }

  [HttpGet("team")]
  public async Task<IActionResult> GetTeam()
  {
    try
    {
      var result = await _userProfileService.GetTeamAsync();
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving team members");
      return StatusCode(500, "An error occurred while retrieving team members.");
    }
  }

  [HttpGet("{userId:guid}/role")]
  public async Task<IActionResult> GetUserRole(Guid userId)
  {
    try
    {
      var result = await _userProfileService.GetUserRoleAsync(userId);
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving role for user {UserId}", userId);
      return StatusCode(500, "An error occurred while retrieving the user role.");
    }
  }
}
