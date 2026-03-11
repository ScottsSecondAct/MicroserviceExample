using ActivityService.Data;
using ActivityService.Models;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Activities.Enums;

namespace ActivityService.Repository;

public class ActivityRepository : IActivityRepository
{
  private readonly ActivityDbContext _context;

  public ActivityRepository(ActivityDbContext context)
  {
    _context = context;
  }

  public async Task<Activity?> GetByIdAsync(Guid id) =>
    await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == id);

  public async Task<List<Activity>> GetAllAsync(Guid? contactId = null, Guid? dealId = null, Guid? accountId = null, Guid? ownerId = null, ActivityType? type = null)
  {
    var query = _context.Activities.AsQueryable();

    if (contactId.HasValue)
      query = query.Where(a => a.ContactId == contactId.Value);

    if (dealId.HasValue)
      query = query.Where(a => a.DealId == dealId.Value);

    if (accountId.HasValue)
      query = query.Where(a => a.AccountId == accountId.Value);

    if (ownerId.HasValue)
      query = query.Where(a => a.OwnerId == ownerId.Value);

    if (type.HasValue)
      query = query.Where(a => a.Type == type.Value);

    return await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
  }

  public async Task AddAsync(Activity activity)
  {
    _context.Activities.Add(activity);
    await _context.SaveChangesAsync();
  }

  public async Task UpdateAsync(Activity activity)
  {
    _context.Activities.Update(activity);
    await _context.SaveChangesAsync();
  }

  public async Task DeleteAsync(Guid id)
  {
    var activity = await _context.Activities.FindAsync(id);
    if (activity != null)
    {
      _context.Activities.Remove(activity);
      await _context.SaveChangesAsync();
    }
  }
}
