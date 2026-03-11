using ActivityService.Models;
using SharedLibrary.Activities.Enums;

namespace ActivityService.Repository;

public interface IActivityRepository
{
  Task<Activity?> GetByIdAsync(Guid id);
  Task<List<Activity>> GetAllAsync(Guid? contactId = null, Guid? dealId = null, Guid? accountId = null, Guid? ownerId = null, ActivityType? type = null);
  Task AddAsync(Activity activity);
  Task UpdateAsync(Activity activity);
  Task DeleteAsync(Guid id);
}
