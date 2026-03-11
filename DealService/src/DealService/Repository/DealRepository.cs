using DealService.Data;
using DealService.Models;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Deals.Enums;

namespace DealService.Repository;

public class DealRepository : IDealRepository
{
  private readonly DealDbContext _context;

  public DealRepository(DealDbContext context)
  {
    _context = context;
  }

  public async Task<Deal?> GetByIdAsync(Guid id) =>
    await _context.Deals.Include(d => d.DealContacts).FirstOrDefaultAsync(d => d.DealId == id);

  public async Task<List<Deal>> GetAllAsync(DealStage? stage = null, Guid? accountId = null, Guid? ownerId = null)
  {
    var query = _context.Deals.Include(d => d.DealContacts).AsQueryable();

    if (stage.HasValue)
      query = query.Where(d => d.Stage == stage.Value);

    if (accountId.HasValue)
      query = query.Where(d => d.AccountId == accountId.Value);

    if (ownerId.HasValue)
      query = query.Where(d => d.OwnerId == ownerId.Value);

    return await query.ToListAsync();
  }

  public async Task AddAsync(Deal deal)
  {
    _context.Deals.Add(deal);
    await _context.SaveChangesAsync();
  }

  public async Task UpdateAsync(Deal deal)
  {
    _context.Deals.Update(deal);
    await _context.SaveChangesAsync();
  }

  public async Task DeleteAsync(Guid id)
  {
    var deal = await _context.Deals.FindAsync(id);
    if (deal != null)
    {
      _context.Deals.Remove(deal);
      await _context.SaveChangesAsync();
    }
  }

  public async Task<DealContact?> GetDealContactAsync(Guid dealId, Guid contactId) =>
    await _context.DealContacts.FirstOrDefaultAsync(dc => dc.DealId == dealId && dc.ContactId == contactId);

  public async Task AddDealContactAsync(DealContact dealContact)
  {
    _context.DealContacts.Add(dealContact);
    await _context.SaveChangesAsync();
  }

  public async Task RemoveDealContactAsync(Guid dealContactId)
  {
    var dc = await _context.DealContacts.FindAsync(dealContactId);
    if (dc != null)
    {
      _context.DealContacts.Remove(dc);
      await _context.SaveChangesAsync();
    }
  }

  public async Task RemoveDealContactsByContactIdAsync(Guid contactId)
  {
    var entries = await _context.DealContacts.Where(dc => dc.ContactId == contactId).ToListAsync();
    if (entries.Count > 0)
    {
      _context.DealContacts.RemoveRange(entries);
      await _context.SaveChangesAsync();
    }
  }
}
