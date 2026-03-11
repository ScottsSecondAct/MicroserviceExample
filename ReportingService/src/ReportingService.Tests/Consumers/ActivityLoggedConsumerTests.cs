using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ReportingService.Consumers;
using ReportingService.Data;
using SharedLibrary.Activities.Enums;
using SharedLibrary.Activities.Events;

namespace ReportingService.Tests.Consumers;

public class ActivityLoggedConsumerTests
{
    private ReportingDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ReportingDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_WithOwnerId_IncrementsRepCount()
    {
        using var db = CreateDb();
        var ownerId = Guid.NewGuid();
        var consumer = new ActivityLoggedConsumer(db);

        await consumer.HandleAsync(new ActivityLogged
        {
            ActivityId = Guid.NewGuid(),
            Type = ActivityType.Call,
            Subject = "Call",
            OwnerId = ownerId
        });

        var proj = await db.ActivityRepProjections.FindAsync(ownerId);
        proj.Should().NotBeNull();
        proj!.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_MultipleActivitiesSameOwner_Accumulates()
    {
        using var db = CreateDb();
        var ownerId = Guid.NewGuid();
        var consumer = new ActivityLoggedConsumer(db);

        await consumer.HandleAsync(new ActivityLogged { ActivityId = Guid.NewGuid(), Type = ActivityType.Call, Subject = "Call 1", OwnerId = ownerId });
        await consumer.HandleAsync(new ActivityLogged { ActivityId = Guid.NewGuid(), Type = ActivityType.Email, Subject = "Email 1", OwnerId = ownerId });
        await consumer.HandleAsync(new ActivityLogged { ActivityId = Guid.NewGuid(), Type = ActivityType.Note, Subject = "Note 1", OwnerId = ownerId });

        var proj = await db.ActivityRepProjections.FindAsync(ownerId);
        proj!.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task HandleAsync_WithoutOwnerId_IsNoOp()
    {
        using var db = CreateDb();
        var consumer = new ActivityLoggedConsumer(db);

        await consumer.HandleAsync(new ActivityLogged
        {
            ActivityId = Guid.NewGuid(),
            Type = ActivityType.Note,
            Subject = "Anonymous note",
            OwnerId = null
        });

        db.ActivityRepProjections.Count().Should().Be(0);
    }
}
