using SharedLibrary.Deals.Enums;
using SharedLibrary.Messaging.Events;
namespace SharedLibrary.Deals.Events;
public record DealStageChanged : BaseEvent
{
  public Guid DealId { get; init; }
  public DealStage OldStage { get; init; }
  public DealStage NewStage { get; init; }
}
