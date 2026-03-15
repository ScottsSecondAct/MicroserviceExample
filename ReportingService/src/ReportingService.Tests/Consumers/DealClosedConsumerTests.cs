using FluentAssertions;
using MassTransit;
using Moq;
using ReportingService.Consumers;
using SharedLibrary.Deals.Events;

namespace ReportingService.Tests.Consumers;

public class DealClosedConsumerTests
{
  [Fact]
  public async Task Consume_CompletesWithoutSideEffects()
  {
    var consumer = new DealClosedConsumer();
    var contextMock = new Mock<ConsumeContext<DealClosed>>();

    var act = async () => await consumer.Consume(contextMock.Object);

    await act.Should().NotThrowAsync();
  }
}
