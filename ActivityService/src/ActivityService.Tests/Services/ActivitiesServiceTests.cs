using ActivityService.Models;
using ActivityService.Models.DTOs;
using ActivityService.Repository;
using ActivityService.Services;
using FluentAssertions;
using MassTransit;
using Moq;
using SharedLibrary.Activities.Enums;
using SharedLibrary.Activities.Events;

namespace ActivityService.Tests.Services;

public class ActivitiesServiceTests
{
  private readonly Mock<IActivityRepository> _mockRepository;
  private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
  private readonly ActivitiesService _service;

  public ActivitiesServiceTests()
  {
    _mockRepository = new Mock<IActivityRepository>();
    _mockPublishEndpoint = new Mock<IPublishEndpoint>();
    _service = new ActivitiesService(_mockRepository.Object, _mockPublishEndpoint.Object);
  }

  private static Activity MakeActivity(ActivityType type = ActivityType.Call, DateTime? completedAt = null) => new()
  {
    ActivityId = Guid.NewGuid(),
    Type = type,
    Subject = "Test Activity",
    Notes = "Some notes",
    OwnerId = Guid.NewGuid(),
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow,
    CompletedAt = completedAt
  };

  // ── CreateActivityAsync ────────────────────────────────────────────────────

  [Fact]
  public async Task CreateActivityAsync_WithValidRequest_ReturnsSuccess()
  {
    var request = new CreateActivityRequest
    {
      Type = ActivityType.Call,
      Subject = "Discovery call",
      Notes = "Initial contact"
    };

    _mockRepository.Setup(r => r.AddAsync(It.IsAny<Activity>())).Returns(Task.CompletedTask);
    _mockPublishEndpoint
      .Setup(p => p.Publish(It.IsAny<ActivityLogged>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    var result = await _service.CreateActivityAsync(request);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(201);
    var response = result.Data as ActivityResponse;
    response.Should().NotBeNull();
    response!.Subject.Should().Be("Discovery call");
    response.Type.Should().Be(ActivityType.Call);
    _mockPublishEndpoint.Verify(
      p => p.Publish(It.IsAny<ActivityLogged>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task CreateActivityAsync_WithEmptySubject_ReturnsFailure()
  {
    var request = new CreateActivityRequest { Type = ActivityType.Email, Subject = "" };

    var result = await _service.CreateActivityAsync(request);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(400);
    _mockRepository.Verify(r => r.AddAsync(It.IsAny<Activity>()), Times.Never);
    _mockPublishEndpoint.Verify(
      p => p.Publish(It.IsAny<ActivityLogged>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task CreateActivityAsync_PublishesActivityLoggedWithCorrectFields()
  {
    var contactId = Guid.NewGuid();
    var dealId = Guid.NewGuid();
    var request = new CreateActivityRequest
    {
      Type = ActivityType.Meeting,
      Subject = "Kickoff meeting",
      ContactId = contactId,
      DealId = dealId
    };

    _mockRepository.Setup(r => r.AddAsync(It.IsAny<Activity>())).Returns(Task.CompletedTask);
    _mockPublishEndpoint
      .Setup(p => p.Publish(It.IsAny<ActivityLogged>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    await _service.CreateActivityAsync(request);

    _mockPublishEndpoint.Verify(
      p => p.Publish(
        It.Is<ActivityLogged>(e =>
          e.Type == ActivityType.Meeting &&
          e.Subject == "Kickoff meeting" &&
          e.ContactId == contactId &&
          e.DealId == dealId),
        It.IsAny<CancellationToken>()),
      Times.Once);
  }

  // ── GetActivityAsync ───────────────────────────────────────────────────────

  [Fact]
  public async Task GetActivityAsync_WhenFound_ReturnsSuccess()
  {
    var activity = MakeActivity();
    _mockRepository.Setup(r => r.GetByIdAsync(activity.ActivityId)).ReturnsAsync(activity);

    var result = await _service.GetActivityAsync(activity.ActivityId);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(200);
    var response = result.Data as ActivityResponse;
    response.Should().NotBeNull();
    response!.ActivityId.Should().Be(activity.ActivityId);
  }

  [Fact]
  public async Task GetActivityAsync_WhenNotFound_ReturnsFailure()
  {
    var id = Guid.NewGuid();
    _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Activity?)null);

    var result = await _service.GetActivityAsync(id);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(404);
  }

  // ── GetAllActivitiesAsync ──────────────────────────────────────────────────

  [Fact]
  public async Task GetAllActivitiesAsync_ReturnsSuccess()
  {
    var activities = new List<Activity> { MakeActivity(), MakeActivity(ActivityType.Email) };
    _mockRepository.Setup(r => r.GetAllAsync(null, null, null, null, null)).ReturnsAsync(activities);

    var result = await _service.GetAllActivitiesAsync();

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(200);
    var response = result.Data as List<ActivityResponse>;
    response.Should().NotBeNull();
    response!.Count.Should().Be(2);
  }

  // ── UpdateActivityAsync ────────────────────────────────────────────────────

  [Fact]
  public async Task UpdateActivityAsync_WhenNotFound_ReturnsFailure()
  {
    var id = Guid.NewGuid();
    _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Activity?)null);

    var result = await _service.UpdateActivityAsync(id, new UpdateActivityRequest());

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(404);
  }

  [Fact]
  public async Task UpdateActivityAsync_UpdatesFields_ReturnsSuccess()
  {
    var activity = MakeActivity();
    var request = new UpdateActivityRequest { Subject = "Updated Subject", Notes = "Updated notes" };

    _mockRepository.Setup(r => r.GetByIdAsync(activity.ActivityId)).ReturnsAsync(activity);
    _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Activity>())).Returns(Task.CompletedTask);

    var result = await _service.UpdateActivityAsync(activity.ActivityId, request);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(200);
    var response = result.Data as ActivityResponse;
    response!.Subject.Should().Be("Updated Subject");
  }

  [Fact]
  public async Task UpdateActivityAsync_WhenTaskCompletedFirstTime_PublishesTaskCompleted()
  {
    var activity = MakeActivity(ActivityType.Task);
    var completedAt = DateTime.UtcNow;
    var request = new UpdateActivityRequest { CompletedAt = completedAt };

    _mockRepository.Setup(r => r.GetByIdAsync(activity.ActivityId)).ReturnsAsync(activity);
    _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Activity>())).Returns(Task.CompletedTask);
    _mockPublishEndpoint
      .Setup(p => p.Publish(It.IsAny<TaskCompleted>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    var result = await _service.UpdateActivityAsync(activity.ActivityId, request);

    result.IsSuccess.Should().BeTrue();
    _mockPublishEndpoint.Verify(
      p => p.Publish(
        It.Is<TaskCompleted>(e => e.ActivityId == activity.ActivityId && e.CompletedAt == completedAt),
        It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task UpdateActivityAsync_WhenAlreadyCompleted_DoesNotPublishTaskCompleted()
  {
    var completedAt = DateTime.UtcNow.AddHours(-1);
    var activity = MakeActivity(ActivityType.Task, completedAt);
    var request = new UpdateActivityRequest { CompletedAt = DateTime.UtcNow };

    _mockRepository.Setup(r => r.GetByIdAsync(activity.ActivityId)).ReturnsAsync(activity);
    _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Activity>())).Returns(Task.CompletedTask);

    await _service.UpdateActivityAsync(activity.ActivityId, request);

    _mockPublishEndpoint.Verify(
      p => p.Publish(It.IsAny<TaskCompleted>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task UpdateActivityAsync_WhenNonTaskTypeCompleted_DoesNotPublishTaskCompleted()
  {
    var activity = MakeActivity(ActivityType.Call);
    var request = new UpdateActivityRequest { CompletedAt = DateTime.UtcNow };

    _mockRepository.Setup(r => r.GetByIdAsync(activity.ActivityId)).ReturnsAsync(activity);
    _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Activity>())).Returns(Task.CompletedTask);

    await _service.UpdateActivityAsync(activity.ActivityId, request);

    _mockPublishEndpoint.Verify(
      p => p.Publish(It.IsAny<TaskCompleted>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  // ── DeleteActivityAsync ────────────────────────────────────────────────────

  [Fact]
  public async Task DeleteActivityAsync_WhenFound_ReturnsSuccess()
  {
    var activity = MakeActivity();
    _mockRepository.Setup(r => r.GetByIdAsync(activity.ActivityId)).ReturnsAsync(activity);
    _mockRepository.Setup(r => r.DeleteAsync(activity.ActivityId)).Returns(Task.CompletedTask);

    var result = await _service.DeleteActivityAsync(activity.ActivityId);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(204);
    _mockRepository.Verify(r => r.DeleteAsync(activity.ActivityId), Times.Once);
  }

  [Fact]
  public async Task DeleteActivityAsync_WhenNotFound_ReturnsFailure()
  {
    var id = Guid.NewGuid();
    _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Activity?)null);

    var result = await _service.DeleteActivityAsync(id);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(404);
    _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
  }
}
