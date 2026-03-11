using ContactService.Models;
using ContactService.Models.DTOs;
using ContactService.Repository;
using MassTransit;
using SharedLibrary.Contacts.Enums;
using SharedLibrary.Contacts.Events;

namespace ContactService.Services;

public class ContactsService : IContactService
{
  private readonly IContactRepository _repository;
  private readonly IPublishEndpoint _publishEndpoint;
  private readonly IAccountClient _accountClient;

  public ContactsService(
    IContactRepository repository,
    IPublishEndpoint publishEndpoint,
    IAccountClient accountClient)
  {
    _repository = repository;
    _publishEndpoint = publishEndpoint;
    _accountClient = accountClient;
  }

  public async Task<ServiceResult> GetAllContactsAsync(ContactStatus? status = null, Guid? ownerId = null, Guid? accountId = null)
  {
    var contacts = await _repository.GetAllAsync(status, ownerId, accountId);
    var response = contacts.Select(MapToResponse).ToList();
    return ServiceResult.Success(response);
  }

  public async Task<ServiceResult> GetContactAsync(Guid id)
  {
    var contact = await _repository.GetByIdAsync(id);
    if (contact == null)
      return ServiceResult.Failure("Contact not found.", 404);

    return ServiceResult.Success(MapToResponse(contact));
  }

  public async Task<ServiceResult> CreateContactAsync(CreateContactRequest request)
  {
    if (request.AccountId.HasValue)
    {
      var accountExists = await _accountClient.AccountExistsAsync(request.AccountId.Value);
      if (!accountExists)
        return ServiceResult.Failure($"Account with ID {request.AccountId.Value} was not found.", 400);
    }

    var contact = new Contact
    {
      ContactId = Guid.NewGuid(),
      FirstName = request.FirstName,
      LastName = request.LastName,
      Email = request.Email,
      Phone = request.Phone,
      Status = request.Status,
      AccountId = request.AccountId,
      OwnerId = request.OwnerId,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    await _repository.AddAsync(contact);

    await _publishEndpoint.Publish(new ContactCreated
    {
      ContactId = contact.ContactId,
      FirstName = contact.FirstName,
      LastName = contact.LastName,
      Email = contact.Email,
      AccountId = contact.AccountId,
      OwnerId = contact.OwnerId
    });

    return ServiceResult.Success(MapToResponse(contact), "Contact created successfully.", 201);
  }

  public async Task<ServiceResult> UpdateContactAsync(Guid id, UpdateContactRequest request)
  {
    var contact = await _repository.GetByIdAsync(id);
    if (contact == null)
      return ServiceResult.Failure("Contact not found.", 404);

    var oldStatus = contact.Status;

    if (request.FirstName != null) contact.FirstName = request.FirstName;
    if (request.LastName != null) contact.LastName = request.LastName;
    if (request.Email != null) contact.Email = request.Email;
    if (request.Phone != null) contact.Phone = request.Phone;
    if (request.Status.HasValue) contact.Status = request.Status.Value;
    if (request.AccountId.HasValue) contact.AccountId = request.AccountId;
    if (request.OwnerId.HasValue) contact.OwnerId = request.OwnerId;
    contact.UpdatedAt = DateTime.UtcNow;

    await _repository.UpdateAsync(contact);

    if (request.Status.HasValue && request.Status.Value != oldStatus)
    {
      await _publishEndpoint.Publish(new ContactStatusChanged
      {
        ContactId = contact.ContactId,
        OldStatus = oldStatus,
        NewStatus = contact.Status
      });
    }

    return ServiceResult.Success(MapToResponse(contact));
  }

  public async Task<ServiceResult> DeleteContactAsync(Guid id)
  {
    var contact = await _repository.GetByIdAsync(id);
    if (contact == null)
      return ServiceResult.Failure("Contact not found.", 404);

    await _repository.DeleteAsync(id);

    await _publishEndpoint.Publish(new ContactDeleted
    {
      ContactId = id
    });

    return ServiceResult.Success(statusCode: 204);
  }

  private static ContactResponse MapToResponse(Contact contact) => new()
  {
    ContactId = contact.ContactId,
    FirstName = contact.FirstName,
    LastName = contact.LastName,
    Email = contact.Email,
    Phone = contact.Phone,
    Status = contact.Status,
    AccountId = contact.AccountId,
    OwnerId = contact.OwnerId,
    CreatedAt = contact.CreatedAt,
    UpdatedAt = contact.UpdatedAt
  };
}
