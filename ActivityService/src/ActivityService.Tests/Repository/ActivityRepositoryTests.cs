using ActivityService.Data;
using ActivityService.Models;
using ActivityService.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Activities.Enums;

namespace ActivityService.Tests.Repository;

public class ActivityRepositoryTests
{
  private static ActivityDbContext MakeContext()
  {
    var options = new DbContextOptionsBuilder<ActivityDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    return new ActivityDbContext(options);
  }

  private static Activity MakeActivity(Guid? contactId = null, Guid? ownerId = null, ActivityType type = ActivityType.Call) => new()
  {
    ActivityId = Guid.NewGuid(),
    Type = type,
    Subject = "Test Activity",
    ContactId = contactId,
    OwnerId = ownerId,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
  };

  [Fact]
  public async Task AddAsync_And_GetByIdAsync_ReturnsActivity()
  {
    using var context = MakeContext();
    var repo = new ActivityRepository(context);
    var activity = MakeActivity();

    await repo.AddAsync(activity);
    var found = await repo.GetByIdAsync(activity.ActivityId);

    found.Should().NotBeNull();
    found!.ActivityId.Should().Be(activity.ActivityId);
    found.Subject.Should().Be("Test Activity");
  }

  [Fact]
  public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
  {
    using var context = MakeContext();
    var repo = new ActivityRepository(context);

    var found = await repo.GetByIdAsync(Guid.NewGuid());

    found.Should().BeNull();
  }

  [Fact]
  public async Task GetAllAsync_WithNoFilters_ReturnsAll()
  {
    using var context = MakeContext();
    var repo = new ActivityRepository(context);
    await repo.AddAsync(MakeActivity());
    await repo.AddAsync(MakeActivity());

    var results = await repo.GetAllAsync();

    results.Count.Should().Be(2);
  }

  [Fact]
  public async Task GetAllAsync_FilterByContactId_ReturnsMatching()
  {
    using var context = MakeContext();
    var repo = new ActivityRepository(context);
    var contactId = Guid.NewGuid();
    await repo.AddAsync(MakeActivity(contactId: contactId));
    await repo.AddAsync(MakeActivity());

    var results = await repo.GetAllAsync(contactId: contactId);

    results.Count.Should().Be(1);
    results[0].ContactId.Should().Be(contactId);
  }

  [Fact]
  public async Task GetAllAsync_FilterByType_ReturnsMatching()
  {
    using var context = MakeContext();
    var repo = new ActivityRepository(context);
    await repo.AddAsync(MakeActivity(type: ActivityType.Task));
    await repo.AddAsync(MakeActivity(type: ActivityType.Email));

    var results = await repo.GetAllAsync(type: ActivityType.Task);

    results.Count.Should().Be(1);
    results[0].Type.Should().Be(ActivityType.Task);
  }

  [Fact]
  public async Task UpdateAsync_PersistsChanges()
  {
    using var context = MakeContext();
    var repo = new ActivityRepository(context);
    var activity = MakeActivity();
    await repo.AddAsync(activity);

    activity.Subject = "Updated Subject";
    await repo.UpdateAsync(activity);

    var found = await repo.GetByIdAsync(activity.ActivityId);
    found!.Subject.Should().Be("Updated Subject");
  }

  [Fact]
  public async Task DeleteAsync_RemovesActivity()
  {
    using var context = MakeContext();
    var repo = new ActivityRepository(context);
    var activity = MakeActivity();
    await repo.AddAsync(activity);

    await repo.DeleteAsync(activity.ActivityId);
    var found = await repo.GetByIdAsync(activity.ActivityId);

    found.Should().BeNull();
  }

  [Fact]
  public async Task DeleteAsync_WhenNotFound_DoesNotThrow()
  {
    using var context = MakeContext();
    var repo = new ActivityRepository(context);

    var act = async () => await repo.DeleteAsync(Guid.NewGuid());

    await act.Should().NotThrowAsync();
  }

  [Fact]
  public async Task GetAllAsync_OrdersByCreatedAtDescending()
  {
    using var context = MakeContext();
    var repo = new ActivityRepository(context);
    var older = MakeActivity();
    older.CreatedAt = DateTime.UtcNow.AddHours(-1);
    var newer = MakeActivity();
    newer.CreatedAt = DateTime.UtcNow;
    await repo.AddAsync(older);
    await repo.AddAsync(newer);

    var results = await repo.GetAllAsync();

    results[0].CreatedAt.Should().BeAfter(results[1].CreatedAt);
  }
}
