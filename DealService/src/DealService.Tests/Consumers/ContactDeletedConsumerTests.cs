using DealService.Consumers;
using DealService.Repository;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using SharedLibrary.Contacts.Events;

namespace DealService.Tests.Consumers;

public class ContactDeletedConsumerTests
{
  [Fact]
  public async Task Consume_RemovesDealContactAssociations()
  {
    var mockRepository = new Mock<IDealRepository>();
    var mockLogger = new Mock<ILogger<ContactDeletedConsumer>>();
    var consumer = new ContactDeletedConsumer(mockRepository.Object, mockLogger.Object);
    var contactId = Guid.NewGuid();

    var mockContext = new Mock<ConsumeContext<ContactDeleted>>();
    mockContext.Setup(c => c.Message).Returns(new ContactDeleted { ContactId = contactId });

    await consumer.Consume(mockContext.Object);

    mockRepository.Verify(r => r.RemoveDealContactsByContactIdAsync(contactId), Times.Once);
  }
}
