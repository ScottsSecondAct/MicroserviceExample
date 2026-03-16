using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementService.Models.DTOs;
using UserManagementService.Services;

namespace UserManagementService.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "admin")]
public class AdminController : ControllerBase
{
  private readonly IUserProfileService _userProfileService;
  private readonly ILogger<AdminController> _logger;

  public AdminController(IUserProfileService userProfileService, ILogger<AdminController> logger)
  {
    _userProfileService = userProfileService;
    _logger = logger;
  }

  [HttpGet("users")]
  public async Task<IActionResult> GetAllUsers()
  {
    try
    {
      var result = await _userProfileService.GetAllUsersAsync();
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving all users");
      return StatusCode(500, "An error occurred while retrieving users.");
    }
  }

  [HttpPut("users/{userId:guid}/role")]
  public async Task<IActionResult> UpdateUserRole(Guid userId, [FromBody] UpdateUserRoleRequest request)
  {
    try
    {
      var result = await _userProfileService.UpdateUserRoleAsync(userId, request.Role);
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating role for user {UserId}", userId);
      return StatusCode(500, "An error occurred while updating the user role.");
    }
  }

  [HttpPut("users/{userId:guid}/active")]
  public async Task<IActionResult> SetUserActive(Guid userId, [FromBody] SetUserActiveRequest request)
  {
    try
    {
      var result = await _userProfileService.SetUserActiveAsync(userId, request.IsActive);
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating active status for user {UserId}", userId);
      return StatusCode(500, "An error occurred while updating the user active status.");
    }
  }
}
