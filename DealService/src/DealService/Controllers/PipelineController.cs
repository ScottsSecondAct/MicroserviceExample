using DealService.Models.DTOs;
using DealService.Repository;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Deals.Enums;

namespace DealService.Controllers;

[ApiController]
[Route("api/pipeline")]
public class PipelineController : ControllerBase
{
  private readonly IDealRepository _repository;
  private readonly ILogger<PipelineController> _logger;

  public PipelineController(IDealRepository repository, ILogger<PipelineController> logger)
  {
    _repository = repository;
    _logger = logger;
  }

  [HttpGet]
  public async Task<IActionResult> GetBoard()
  {
    try
    {
      var deals = await _repository.GetAllAsync();
      var board = Enum.GetValues<DealStage>().Select(stage => new
      {
        stage = stage.ToString(),
        deals = deals.Where(d => d.Stage == stage).Select(d => new DealResponse
        {
          DealId = d.DealId,
          Title = d.Title,
          AccountId = d.AccountId,
          Stage = d.Stage,
          Value = d.Value,
          Probability = d.Probability,
          ExpectedCloseDate = d.ExpectedCloseDate,
          OwnerId = d.OwnerId,
          CreatedAt = d.CreatedAt,
          UpdatedAt = d.UpdatedAt,
          Contacts = d.DealContacts.Select(dc => new DealContactResponse
          {
            DealContactId = dc.DealContactId,
            ContactId = dc.ContactId,
            Role = dc.Role
          }).ToList()
        }).ToList(),
        totalValue = deals.Where(d => d.Stage == stage).Sum(d => d.Value)
      }).ToList();

      return Ok(board);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving pipeline board");
      return StatusCode(500, "An error occurred while retrieving the pipeline board.");
    }
  }
}
