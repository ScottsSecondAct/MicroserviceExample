using ActivityService.Models.DTOs;
using ActivityService.Services;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Activities.Enums;

namespace ActivityService.Controllers;

[ApiController]
[Route("api/activities")]
public class ActivitiesController : ControllerBase
{
  private readonly IActivityService _activityService;
  private readonly ILogger<ActivitiesController> _logger;

  public ActivitiesController(IActivityService activityService, ILogger<ActivitiesController> logger)
  {
    _activityService = activityService;
    _logger = logger;
  }

  [HttpGet]
  public async Task<IActionResult> GetAll(
    [FromQuery] Guid? contactId,
    [FromQuery] Guid? dealId,
    [FromQuery] Guid? accountId,
    [FromQuery] Guid? ownerId,
    [FromQuery] ActivityType? type)
  {
    try
    {
      var result = await _activityService.GetAllActivitiesAsync(contactId, dealId, accountId, ownerId, type);
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving activities");
      return StatusCode(500, "An error occurred while retrieving activities.");
    }
  }

  [HttpPost]
  public async Task<IActionResult> Create([FromBody] CreateActivityRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.Subject))
      return BadRequest("Subject is required.");

    try
    {
      var result = await _activityService.CreateActivityAsync(request);
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error creating activity");
      return StatusCode(500, "An error occurred while creating the activity.");
    }
  }

  [HttpPut("{id:guid}")]
  public async Task<IActionResult> Update(Guid id, [FromBody] UpdateActivityRequest request)
  {
    try
    {
      var result = await _activityService.UpdateActivityAsync(id, request);
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating activity {ActivityId}", id);
      return StatusCode(500, "An error occurred while updating the activity.");
    }
  }

  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> Delete(Guid id)
  {
    try
    {
      var result = await _activityService.DeleteActivityAsync(id);
      if (!result.IsSuccess)
        return StatusCode(result.StatusCode, result.Message);
      return NoContent();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error deleting activity {ActivityId}", id);
      return StatusCode(500, "An error occurred while deleting the activity.");
    }
  }
}
