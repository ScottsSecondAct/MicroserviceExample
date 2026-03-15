using SharedLibrary.Deals.Enums;

namespace DealService.Models;

public class Deal
{
  public Guid DealId { get; set; }
  public string Title { get; set; } = string.Empty;
  public Guid? AccountId { get; set; }
  public DealStage Stage { get; set; } = DealStage.Prospecting;
  public decimal Value { get; set; }
  public int? Probability { get; set; }
  public DateTime? ExpectedCloseDate { get; set; }
  public Guid? OwnerId { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
  public bool IsDeleted { get; set; }
  public DateTime? DeletedAt { get; set; }
  public Guid? DeletedBy { get; set; }
  public ICollection<DealContact> DealContacts { get; set; } = new List<DealContact>();
}
