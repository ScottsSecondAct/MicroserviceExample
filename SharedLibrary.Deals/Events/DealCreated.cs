using SharedLibrary.Deals.Enums;
using SharedLibrary.Messaging.Events;
namespace SharedLibrary.Deals.Events;
public record DealCreated : BaseEvent
{
  public Guid DealId { get; init; }
  public string Title { get; init; } = "";
  public Guid? AccountId { get; init; }
  public DealStage Stage { get; init; }
  public decimal Value { get; init; }
}
