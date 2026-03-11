using AccountService.Models.DTOs;
using AccountService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
  private readonly IAccountService _accountService;
  private readonly ILogger<AccountsController> _logger;

  public AccountsController(IAccountService accountService, ILogger<AccountsController> logger)
  {
    _accountService = accountService;
    _logger = logger;
  }

  [HttpGet]
  public async Task<IActionResult> GetAll()
  {
    try
    {
      var result = await _accountService.GetAllAccountsAsync();
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving accounts");
      return StatusCode(500, "An error occurred while retrieving accounts.");
    }
  }

  [HttpGet("{id:guid}")]
  public async Task<IActionResult> GetById(Guid id)
  {
    try
    {
      var result = await _accountService.GetAccountAsync(id);
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving account {AccountId}", id);
      return StatusCode(500, "An error occurred while retrieving the account.");
    }
  }

  [HttpPost]
  public async Task<IActionResult> Create([FromBody] CreateAccountRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.Name))
      return BadRequest("Name is required.");

    try
    {
      var result = await _accountService.CreateAccountAsync(request);
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error creating account");
      return StatusCode(500, "An error occurred while creating the account.");
    }
  }

  [HttpPut("{id:guid}")]
  public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAccountRequest request)
  {
    try
    {
      var result = await _accountService.UpdateAccountAsync(id, request);
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating account {AccountId}", id);
      return StatusCode(500, "An error occurred while updating the account.");
    }
  }

  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> Delete(Guid id)
  {
    try
    {
      var result = await _accountService.DeleteAccountAsync(id);
      if (!result.IsSuccess)
        return StatusCode(result.StatusCode, result.Message);
      return NoContent();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error deleting account {AccountId}", id);
      return StatusCode(500, "An error occurred while deleting the account.");
    }
  }
}
