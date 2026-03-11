using DealService.Models.DTOs;
using DealService.Services;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Deals.Enums;

namespace DealService.Controllers;

[ApiController]
[Route("api/deals")]
public class DealsController : ControllerBase
{
  private readonly IDealService _dealService;
  private readonly ILogger<DealsController> _logger;

  public DealsController(IDealService dealService, ILogger<DealsController> logger)
  {
    _dealService = dealService;
    _logger = logger;
  }

  [HttpGet]
  public async Task<IActionResult> GetAll([FromQuery] DealStage? stage, [FromQuery] Guid? accountId, [FromQuery] Guid? ownerId)
  {
    try
    {
      var result = await _dealService.GetAllDealsAsync(stage, accountId, ownerId);
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving deals");
      return StatusCode(500, "An error occurred while retrieving deals.");
    }
  }

  [HttpGet("{id:guid}")]
  public async Task<IActionResult> GetById(Guid id)
  {
    try
    {
      var result = await _dealService.GetDealAsync(id);
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving deal {DealId}", id);
      return StatusCode(500, "An error occurred while retrieving the deal.");
    }
  }

  [HttpPost]
  public async Task<IActionResult> Create([FromBody] CreateDealRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.Title))
      return BadRequest("Title is required.");

    try
    {
      var result = await _dealService.CreateDealAsync(request);
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error creating deal");
      return StatusCode(500, "An error occurred while creating the deal.");
    }
  }

  [HttpPut("{id:guid}")]
  public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDealRequest request)
  {
    try
    {
      var result = await _dealService.UpdateDealAsync(id, request);
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating deal {DealId}", id);
      return StatusCode(500, "An error occurred while updating the deal.");
    }
  }

  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> Delete(Guid id)
  {
    try
    {
      var result = await _dealService.DeleteDealAsync(id);
      if (!result.IsSuccess)
        return StatusCode(result.StatusCode, result.Message);
      return NoContent();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error deleting deal {DealId}", id);
      return StatusCode(500, "An error occurred while deleting the deal.");
    }
  }

  [HttpPost("{id:guid}/contacts")]
  public async Task<IActionResult> AddContact(Guid id, [FromBody] AddDealContactRequest request)
  {
    try
    {
      var result = await _dealService.AddContactToDealAsync(id, request);
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error adding contact to deal {DealId}", id);
      return StatusCode(500, "An error occurred while adding the contact.");
    }
  }

  [HttpDelete("{id:guid}/contacts/{contactId:guid}")]
  public async Task<IActionResult> RemoveContact(Guid id, Guid contactId)
  {
    try
    {
      var result = await _dealService.RemoveContactFromDealAsync(id, contactId);
      if (!result.IsSuccess)
        return StatusCode(result.StatusCode, result.Message);
      return NoContent();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error removing contact from deal {DealId}", id);
      return StatusCode(500, "An error occurred while removing the contact.");
    }
  }
}
