using MassTransit;
using SharedLibrary.Deals.Events;

namespace ReportingService.Consumers;

// DealClosed fires alongside DealStageChanged when a deal reaches ClosedWon.
// Pipeline projection updates are handled entirely by DealStageChangedConsumer,
// so this consumer intentionally takes no action to avoid double-counting.
// Reserved for future metrics (e.g. closed ARR, win-rate calculations).
public class DealClosedConsumer : IConsumer<DealClosed>
{
    public Task Consume(ConsumeContext<DealClosed> context) => Task.CompletedTask;
}
