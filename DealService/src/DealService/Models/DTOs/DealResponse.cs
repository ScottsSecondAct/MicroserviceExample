using SharedLibrary.Deals.Enums;

namespace DealService.Models.DTOs;

public class DealResponse
{
  public Guid DealId { get; set; }
  public string Title { get; set; } = string.Empty;
  public Guid? AccountId { get; set; }
  public DealStage Stage { get; set; }
  public decimal Value { get; set; }
  public int? Probability { get; set; }
  public DateTime? ExpectedCloseDate { get; set; }
  public Guid? OwnerId { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
  public List<DealContactResponse> Contacts { get; set; } = new();
}

public class DealContactResponse
{
  public Guid DealContactId { get; set; }
  public Guid ContactId { get; set; }
  public DealContactRole Role { get; set; }
}
