using SharedLibrary.Deals.Enums;
using SharedLibrary.Messaging.Events;
namespace SharedLibrary.Deals.Events;
public record DealClosed : BaseEvent
{
  public Guid DealId { get; init; }
  public DealStage Stage { get; init; }
  public decimal Value { get; init; }
}
