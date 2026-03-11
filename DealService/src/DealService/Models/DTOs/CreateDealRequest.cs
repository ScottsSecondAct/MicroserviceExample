using SharedLibrary.Deals.Enums;

namespace DealService.Models.DTOs;

public class CreateDealRequest
{
  public string Title { get; set; } = string.Empty;
  public Guid? AccountId { get; set; }
  public DealStage Stage { get; set; } = DealStage.Prospecting;
  public decimal Value { get; set; }
  public int? Probability { get; set; }
  public DateTime? ExpectedCloseDate { get; set; }
  public Guid? OwnerId { get; set; }
}
