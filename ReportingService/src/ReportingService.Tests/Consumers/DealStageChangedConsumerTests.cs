using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ReportingService.Consumers;
using ReportingService.Data;
using ReportingService.Models;
using SharedLibrary.Deals.Enums;
using SharedLibrary.Deals.Events;

namespace ReportingService.Tests.Consumers;

public class DealStageChangedConsumerTests
{
    private ReportingDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ReportingDbContext(options);
    }

    private async Task SeedDeal(ReportingDbContext db, Guid dealId, DealStage stage, decimal value)
    {
        db.DealSnapshots.Add(new DealSnapshot { DealId = dealId, Stage = stage.ToString(), Value = value });
        db.PipelineProjections.Add(new PipelineProjection { Stage = stage.ToString(), DealCount = 1, TotalValue = value });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task HandleAsync_MovesValueFromOldStageToNewStage()
    {
        using var db = CreateDb();
        var dealId = Guid.NewGuid();
        await SeedDeal(db, dealId, DealStage.Prospecting, 5000);

        var consumer = new DealStageChangedConsumer(db);
        await consumer.HandleAsync(new DealStageChanged
        {
            DealId = dealId,
            OldStage = DealStage.Prospecting,
            NewStage = DealStage.Proposal
        });

        var oldProj = await db.PipelineProjections.FindAsync("Prospecting");
        oldProj!.DealCount.Should().Be(0);
        oldProj.TotalValue.Should().Be(0);

        var newProj = await db.PipelineProjections.FindAsync("Proposal");
        newProj.Should().NotBeNull();
        newProj!.DealCount.Should().Be(1);
        newProj.TotalValue.Should().Be(5000);

        var snapshot = await db.DealSnapshots.FindAsync(dealId);
        snapshot!.Stage.Should().Be("Proposal");
    }

    [Fact]
    public async Task HandleAsync_UnknownDealId_IsNoOp()
    {
        using var db = CreateDb();
        var consumer = new DealStageChangedConsumer(db);

        // Should not throw
        await consumer.HandleAsync(new DealStageChanged
        {
            DealId = Guid.NewGuid(),
            OldStage = DealStage.Prospecting,
            NewStage = DealStage.Proposal
        });

        db.PipelineProjections.Count().Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_CountNeverGoesBelowZero()
    {
        using var db = CreateDb();
        var dealId = Guid.NewGuid();
        // Seed with zero count (edge case)
        db.DealSnapshots.Add(new DealSnapshot { DealId = dealId, Stage = "Prospecting", Value = 1000 });
        db.PipelineProjections.Add(new PipelineProjection { Stage = "Prospecting", DealCount = 0, TotalValue = 0 });
        await db.SaveChangesAsync();

        var consumer = new DealStageChangedConsumer(db);
        await consumer.HandleAsync(new DealStageChanged
        {
            DealId = dealId,
            OldStage = DealStage.Prospecting,
            NewStage = DealStage.Proposal
        });

        var oldProj = await db.PipelineProjections.FindAsync("Prospecting");
        oldProj!.DealCount.Should().Be(0);
        oldProj.TotalValue.Should().Be(0);
    }
}
