using ContactService.Models.DTOs;
using ContactService.Services;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Contacts.Enums;

namespace ContactService.Controllers;

[ApiController]
[Route("api/contacts")]
public class ContactsController : ControllerBase
{
  private readonly IContactService _contactService;
  private readonly ILogger<ContactsController> _logger;

  public ContactsController(IContactService contactService, ILogger<ContactsController> logger)
  {
    _contactService = contactService;
    _logger = logger;
  }

  [HttpGet]
  public async Task<IActionResult> GetAll([FromQuery] ContactStatus? status, [FromQuery] Guid? ownerId, [FromQuery] Guid? accountId)
  {
    try
    {
      var result = await _contactService.GetAllContactsAsync(status, ownerId, accountId);
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving contacts");
      return StatusCode(500, "An error occurred while retrieving contacts.");
    }
  }

  [HttpGet("{id:guid}")]
  public async Task<IActionResult> GetById(Guid id)
  {
    try
    {
      var result = await _contactService.GetContactAsync(id);
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving contact {ContactId}", id);
      return StatusCode(500, "An error occurred while retrieving the contact.");
    }
  }

  [HttpPost]
  public async Task<IActionResult> Create([FromBody] CreateContactRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
      return BadRequest("FirstName and LastName are required.");

    if (string.IsNullOrWhiteSpace(request.Email))
      return BadRequest("Email is required.");

    try
    {
      var result = await _contactService.CreateContactAsync(request);
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error creating contact");
      return StatusCode(500, "An error occurred while creating the contact.");
    }
  }

  [HttpPut("{id:guid}")]
  public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContactRequest request)
  {
    try
    {
      var result = await _contactService.UpdateContactAsync(id, request);
      return StatusCode(result.StatusCode, result.Data ?? result.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating contact {ContactId}", id);
      return StatusCode(500, "An error occurred while updating the contact.");
    }
  }

  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> Delete(Guid id)
  {
    try
    {
      var result = await _contactService.DeleteContactAsync(id);
      if (!result.IsSuccess)
        return StatusCode(result.StatusCode, result.Message);
      return NoContent();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error deleting contact {ContactId}", id);
      return StatusCode(500, "An error occurred while deleting the contact.");
    }
  }
}
