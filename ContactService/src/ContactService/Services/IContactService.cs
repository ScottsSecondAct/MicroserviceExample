using ContactService.Models.DTOs;
using SharedLibrary.Contacts.Enums;

namespace ContactService.Services;

public interface IContactService
{
  Task<ServiceResult> GetAllContactsAsync(ContactStatus? status = null, Guid? ownerId = null, Guid? accountId = null);
  Task<ServiceResult> GetContactAsync(Guid id);
  Task<ServiceResult> CreateContactAsync(CreateContactRequest request);
  Task<ServiceResult> UpdateContactAsync(Guid id, UpdateContactRequest request);
  Task<ServiceResult> DeleteContactAsync(Guid id);
}
