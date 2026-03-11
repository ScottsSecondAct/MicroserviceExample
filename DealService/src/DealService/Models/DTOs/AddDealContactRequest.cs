using SharedLibrary.Deals.Enums;

namespace DealService.Models.DTOs;

public class AddDealContactRequest
{
  public Guid ContactId { get; set; }
  public DealContactRole Role { get; set; } = DealContactRole.Influencer;
}
