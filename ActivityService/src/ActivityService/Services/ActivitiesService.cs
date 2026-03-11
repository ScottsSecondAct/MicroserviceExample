using ActivityService.Models;
using ActivityService.Models.DTOs;
using ActivityService.Repository;
using MassTransit;
using SharedLibrary.Activities.Enums;
using SharedLibrary.Activities.Events;

namespace ActivityService.Services;

public class ActivitiesService : IActivityService
{
  private readonly IActivityRepository _repository;
  private readonly IPublishEndpoint _publishEndpoint;

  public ActivitiesService(IActivityRepository repository, IPublishEndpoint publishEndpoint)
  {
    _repository = repository;
    _publishEndpoint = publishEndpoint;
  }

  public async Task<ServiceResult> GetAllActivitiesAsync(Guid? contactId = null, Guid? dealId = null, Guid? accountId = null, Guid? ownerId = null, ActivityType? type = null)
  {
    var activities = await _repository.GetAllAsync(contactId, dealId, accountId, ownerId, type);
    return ServiceResult.Success(activities.Select(MapToResponse).ToList());
  }

  public async Task<ServiceResult> GetActivityAsync(Guid id)
  {
    var activity = await _repository.GetByIdAsync(id);
    if (activity == null)
      return ServiceResult.Failure("Activity not found.", 404);
    return ServiceResult.Success(MapToResponse(activity));
  }

  public async Task<ServiceResult> CreateActivityAsync(CreateActivityRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.Subject))
      return ServiceResult.Failure("Subject is required.", 400);

    var activity = new Activity
    {
      ActivityId = Guid.NewGuid(),
      Type = request.Type,
      Subject = request.Subject,
      Notes = request.Notes,
      ContactId = request.ContactId,
      DealId = request.DealId,
      AccountId = request.AccountId,
      OwnerId = request.OwnerId,
      ScheduledAt = request.ScheduledAt,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    await _repository.AddAsync(activity);

    await _publishEndpoint.Publish(new ActivityLogged
    {
      ActivityId = activity.ActivityId,
      Type = activity.Type,
      Subject = activity.Subject,
      ContactId = activity.ContactId,
      DealId = activity.DealId,
      AccountId = activity.AccountId,
      OwnerId = activity.OwnerId
    });

    return ServiceResult.Success(MapToResponse(activity), "Activity created successfully.", 201);
  }

  public async Task<ServiceResult> UpdateActivityAsync(Guid id, UpdateActivityRequest request)
  {
    var activity = await _repository.GetByIdAsync(id);
    if (activity == null)
      return ServiceResult.Failure("Activity not found.", 404);

    if (request.Type.HasValue) activity.Type = request.Type.Value;
    if (request.Subject != null) activity.Subject = request.Subject;
    if (request.Notes != null) activity.Notes = request.Notes;
    if (request.ContactId.HasValue) activity.ContactId = request.ContactId;
    if (request.DealId.HasValue) activity.DealId = request.DealId;
    if (request.AccountId.HasValue) activity.AccountId = request.AccountId;
    if (request.OwnerId.HasValue) activity.OwnerId = request.OwnerId;
    if (request.ScheduledAt.HasValue) activity.ScheduledAt = request.ScheduledAt;

    var wasCompleted = activity.CompletedAt.HasValue;
    if (request.CompletedAt.HasValue)
      activity.CompletedAt = request.CompletedAt;

    activity.UpdatedAt = DateTime.UtcNow;

    await _repository.UpdateAsync(activity);

    if (!wasCompleted && activity.CompletedAt.HasValue && activity.Type == ActivityType.Task)
    {
      await _publishEndpoint.Publish(new TaskCompleted
      {
        ActivityId = activity.ActivityId,
        Subject = activity.Subject,
        OwnerId = activity.OwnerId,
        CompletedAt = activity.CompletedAt.Value
      });
    }

    return ServiceResult.Success(MapToResponse(activity));
  }

  public async Task<ServiceResult> DeleteActivityAsync(Guid id)
  {
    var activity = await _repository.GetByIdAsync(id);
    if (activity == null)
      return ServiceResult.Failure("Activity not found.", 404);

    await _repository.DeleteAsync(id);
    return ServiceResult.Success(statusCode: 204);
  }

  private static ActivityResponse MapToResponse(Activity activity) => new()
  {
    ActivityId = activity.ActivityId,
    Type = activity.Type,
    Subject = activity.Subject,
    Notes = activity.Notes,
    ContactId = activity.ContactId,
    DealId = activity.DealId,
    AccountId = activity.AccountId,
    OwnerId = activity.OwnerId,
    ScheduledAt = activity.ScheduledAt,
    CompletedAt = activity.CompletedAt,
    CreatedAt = activity.CreatedAt,
    UpdatedAt = activity.UpdatedAt
  };
}
