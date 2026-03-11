using SharedLibrary.Contacts.Enums;

namespace ContactService.Models.DTOs;

public class CreateContactRequest
{
  public string FirstName { get; set; } = string.Empty;
  public string LastName { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
  public string? Phone { get; set; }
  public ContactStatus Status { get; set; } = ContactStatus.Lead;
  public Guid? AccountId { get; set; }
  public Guid? OwnerId { get; set; }
}
