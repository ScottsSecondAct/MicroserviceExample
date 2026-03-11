using SharedLibrary.Deals.Enums;

namespace DealService.Models.DTOs;

public class UpdateDealRequest
{
  public string? Title { get; set; }
  public Guid? AccountId { get; set; }
  public DealStage? Stage { get; set; }
  public decimal? Value { get; set; }
  public int? Probability { get; set; }
  public DateTime? ExpectedCloseDate { get; set; }
  public Guid? OwnerId { get; set; }
}
