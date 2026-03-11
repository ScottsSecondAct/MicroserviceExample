using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ReportingService.Consumers;
using ReportingService.Data;
using ReportingService.Models;
using SharedLibrary.Deals.Enums;
using SharedLibrary.Deals.Events;

namespace ReportingService.Tests.Consumers;

public class DealCreatedConsumerTests
{
    private ReportingDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ReportingDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_CreatesDealSnapshot_And_UpdatesPipelineProjection()
    {
        using var db = CreateDb();
        var consumer = new DealCreatedConsumer(db);
        var dealId = Guid.NewGuid();

        await consumer.HandleAsync(new DealCreated
        {
            DealId = dealId,
            Title = "New Deal",
            Stage = DealStage.Prospecting,
            Value = 10000
        });

        var snapshot = await db.DealSnapshots.FindAsync(dealId);
        snapshot.Should().NotBeNull();
        snapshot!.Stage.Should().Be("Prospecting");
        snapshot.Value.Should().Be(10000);

        var proj = await db.PipelineProjections.FindAsync("Prospecting");
        proj.Should().NotBeNull();
        proj!.DealCount.Should().Be(1);
        proj.TotalValue.Should().Be(10000);
    }

    [Fact]
    public async Task HandleAsync_MultipleDealsSameStage_AccumulatesCorrectly()
    {
        using var db = CreateDb();
        var consumer = new DealCreatedConsumer(db);

        await consumer.HandleAsync(new DealCreated { DealId = Guid.NewGuid(), Stage = DealStage.Proposal, Value = 5000 });
        await consumer.HandleAsync(new DealCreated { DealId = Guid.NewGuid(), Stage = DealStage.Proposal, Value = 3000 });

        var proj = await db.PipelineProjections.FindAsync("Proposal");
        proj!.DealCount.Should().Be(2);
        proj.TotalValue.Should().Be(8000);
    }

    [Fact]
    public async Task HandleAsync_DuplicateDealId_IsIdempotent()
    {
        using var db = CreateDb();
        var consumer = new DealCreatedConsumer(db);
        var dealId = Guid.NewGuid();

        var e = new DealCreated { DealId = dealId, Stage = DealStage.Prospecting, Value = 1000 };
        await consumer.HandleAsync(e);
        await consumer.HandleAsync(e); // duplicate

        db.DealSnapshots.Count().Should().Be(1);
        var proj = await db.PipelineProjections.FindAsync("Prospecting");
        proj!.DealCount.Should().Be(1);
        proj.TotalValue.Should().Be(1000);
    }
}
