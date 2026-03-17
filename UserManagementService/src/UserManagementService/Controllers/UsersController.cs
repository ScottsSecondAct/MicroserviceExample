using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.DTOs;
using SharedLibrary.Enums;
using UserManagementService.Models.DTOs;
using UserManagementService.Services;

namespace UserManagementService.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
  private readonly IUserProfileService _userProfileService;
  private readonly IAuditLogService _auditLogService;
  private readonly ILogger<UsersController> _logger;

  public UsersController(IUserProfileService userProfileService, IAuditLogService auditLogService, ILogger<UsersController> logger)
  {
    _userProfileService = userProfileService;
    _auditLogService = auditLogService;
    _logger = logger;
  }

  private Guid GetActorUserId() =>
    Guid.TryParse(User.FindFirstValue("UserId"), out var id) ? id : Guid.Empty;

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

  [HttpPatch("{userId:guid}/status")]
  [Authorize(Policy = "admin")]
  public async Task<IActionResult> PatchUserStatus(Guid userId, [FromBody] SetUserActiveRequest request)
  {
    try
    {
      var result = await _userProfileService.SetUserActiveAsync(userId, request.IsActive, GetActorUserId());
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating status for user {UserId}", userId);
      return StatusCode(500, "An error occurred while updating the user status.");
    }
  }

  [HttpPatch("{userId:guid}/role")]
  [Authorize(Policy = "admin")]
  public async Task<IActionResult> PatchUserRole(Guid userId, [FromBody] UpdateUserRoleRequest request)
  {
    if (!Enum.IsDefined(typeof(UserRole), request.Role))
      return BadRequest("Invalid role value.");

    try
    {
      var result = await _userProfileService.UpdateUserRoleAsync(userId, request.Role, GetActorUserId());
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating role for user {UserId}", userId);
      return StatusCode(500, "An error occurred while updating the user role.");
    }
  }

  [HttpPost("{userId:guid}/resend-invite")]
  [Authorize(Policy = "admin")]
  public async Task<IActionResult> ResendInvite(Guid userId)
  {
    try
    {
      var result = await _userProfileService.ResendInviteAsync(userId, GetActorUserId());
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error resending invite for user {UserId}", userId);
      return StatusCode(500, "An error occurred while resending the invite.");
    }
  }

  [HttpGet("audit")]
  [Authorize(Policy = "admin")]
  public async Task<IActionResult> GetAuditLog()
  {
    try
    {
      var entries = await _auditLogService.GetAuditLogsAsync();
      return Ok(entries);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving audit log");
      return StatusCode(500, "An error occurred while retrieving the audit log.");
    }
  }
}
