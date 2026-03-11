using DealService.Repository;
using MassTransit;
using SharedLibrary.Contacts.Events;

namespace DealService.Consumers;

public class ContactDeletedConsumer : IConsumer<ContactDeleted>
{
  private readonly IDealRepository _repository;
  private readonly ILogger<ContactDeletedConsumer> _logger;

  public ContactDeletedConsumer(IDealRepository repository, ILogger<ContactDeletedConsumer> logger)
  {
    _repository = repository;
    _logger = logger;
  }

  public async Task Consume(ConsumeContext<ContactDeleted> context)
  {
    var contactId = context.Message.ContactId;
    _logger.LogInformation("Handling ContactDeleted for {ContactId}: removing deal associations.", contactId);
    await _repository.RemoveDealContactsByContactIdAsync(contactId);
  }
}
