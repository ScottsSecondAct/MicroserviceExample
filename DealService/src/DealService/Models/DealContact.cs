using SharedLibrary.Deals.Enums;

namespace DealService.Models;

public class DealContact
{
  public Guid DealContactId { get; set; }
  public Guid DealId { get; set; }
  public Guid ContactId { get; set; }
  public DealContactRole Role { get; set; } = DealContactRole.Influencer;
  public DateTime CreatedAt { get; set; }
  public Deal Deal { get; set; } = null!;
}
