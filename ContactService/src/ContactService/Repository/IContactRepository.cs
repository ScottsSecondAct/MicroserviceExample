using ContactService.Models;
using SharedLibrary.Contacts.Enums;

namespace ContactService.Repository;

public interface IContactRepository
{
  Task<Contact?> GetByIdAsync(Guid id);
  Task<List<Contact>> GetAllAsync(ContactStatus? status = null, Guid? ownerId = null, Guid? accountId = null);
  Task<List<Contact>> GetByAccountIdAsync(Guid accountId);
  Task AddAsync(Contact contact);
  Task UpdateAsync(Contact contact);
  Task DeleteAsync(Guid id);
}
