using MassTransit;
using ReportingService.Data;
using ReportingService.Models;
using SharedLibrary.Deals.Events;

namespace ReportingService.Consumers;

public class DealStageChangedConsumer : IConsumer<DealStageChanged>
{
    private readonly ReportingDbContext _db;

    public DealStageChangedConsumer(ReportingDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<DealStageChanged> context) =>
        await HandleAsync(context.Message);

    public async Task HandleAsync(DealStageChanged e)
    {
        var snapshot = await _db.DealSnapshots.FindAsync(e.DealId);
        if (snapshot == null) return; // unknown deal — skip

        var oldStage = e.OldStage.ToString();
        var newStage = e.NewStage.ToString();

        var oldProj = await _db.PipelineProjections.FindAsync(oldStage);
        if (oldProj != null)
        {
            oldProj.DealCount = Math.Max(0, oldProj.DealCount - 1);
            oldProj.TotalValue = Math.Max(0, oldProj.TotalValue - snapshot.Value);
            oldProj.UpdatedAt = DateTime.UtcNow;
        }

        var newProj = await _db.PipelineProjections.FindAsync(newStage);
        if (newProj == null)
        {
            newProj = new PipelineProjection { Stage = newStage };
            _db.PipelineProjections.Add(newProj);
        }
        newProj.DealCount++;
        newProj.TotalValue += snapshot.Value;
        newProj.UpdatedAt = DateTime.UtcNow;

        snapshot.Stage = newStage;
        snapshot.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }
}
