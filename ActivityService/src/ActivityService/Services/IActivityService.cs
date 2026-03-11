using ActivityService.Models.DTOs;
using SharedLibrary.Activities.Enums;

namespace ActivityService.Services;

public interface IActivityService
{
  Task<ServiceResult> GetAllActivitiesAsync(Guid? contactId = null, Guid? dealId = null, Guid? accountId = null, Guid? ownerId = null, ActivityType? type = null);
  Task<ServiceResult> GetActivityAsync(Guid id);
  Task<ServiceResult> CreateActivityAsync(CreateActivityRequest request);
  Task<ServiceResult> UpdateActivityAsync(Guid id, UpdateActivityRequest request);
  Task<ServiceResult> DeleteActivityAsync(Guid id);
}
