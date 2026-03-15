using ContactService.Data;
using ContactService.Models;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contacts.Enums;

namespace ContactService.Repository;

public class ContactRepository : IContactRepository
{
  private readonly ContactDbContext _context;

  public ContactRepository(ContactDbContext context)
  {
    _context = context;
  }

  public async Task<Contact?> GetByIdAsync(Guid id) =>
    await _context.Contacts.FirstOrDefaultAsync(c => c.ContactId == id);

  public async Task<List<Contact>> GetAllAsync(ContactStatus? status = null, Guid? ownerId = null, Guid? accountId = null)
  {
    var query = _context.Contacts.AsQueryable();

    if (status.HasValue)
      query = query.Where(c => c.Status == status.Value);

    if (ownerId.HasValue)
      query = query.Where(c => c.OwnerId == ownerId.Value);

    if (accountId.HasValue)
      query = query.Where(c => c.AccountId == accountId.Value);

    return await query.ToListAsync();
  }

  public async Task<List<Contact>> GetByAccountIdAsync(Guid accountId) =>
    await _context.Contacts.Where(c => c.AccountId == accountId).ToListAsync();

  public async Task AddAsync(Contact contact)
  {
    _context.Contacts.Add(contact);
    await _context.SaveChangesAsync();
  }

  public async Task UpdateAsync(Contact contact)
  {
    _context.Contacts.Update(contact);
    await _context.SaveChangesAsync();
  }

  public async Task DeleteAsync(Guid id)
  {
    var contact = await _context.Contacts.FindAsync(id);
    if (contact != null)
    {
      contact.IsDeleted = true;
      contact.DeletedAt = DateTime.UtcNow;
      await _context.SaveChangesAsync();
    }
  }
}
