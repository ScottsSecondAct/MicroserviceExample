using SharedLibrary.Contacts.Enums;

namespace ContactService.Models;

public class Contact
{
  public Guid ContactId { get; set; }
  public string FirstName { get; set; } = string.Empty;
  public string LastName { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
  public string? Phone { get; set; }
  public ContactStatus Status { get; set; } = ContactStatus.Lead;
  public Guid? AccountId { get; set; }
  public Guid? OwnerId { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
}
