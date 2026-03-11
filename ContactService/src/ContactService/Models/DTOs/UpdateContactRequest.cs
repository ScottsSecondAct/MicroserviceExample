using SharedLibrary.Contacts.Enums;

namespace ContactService.Models.DTOs;

public class UpdateContactRequest
{
  public string? FirstName { get; set; }
  public string? LastName { get; set; }
  public string? Email { get; set; }
  public string? Phone { get; set; }
  public ContactStatus? Status { get; set; }
  public Guid? AccountId { get; set; }
  public Guid? OwnerId { get; set; }
}
