using MassTransit;
using Microsoft.EntityFrameworkCore;
using ReportingService.Data;
using ReportingService.Models;
using SharedLibrary.Deals.Events;

namespace ReportingService.Consumers;

public class DealCreatedConsumer : IConsumer<DealCreated>
{
    private readonly ReportingDbContext _db;

    public DealCreatedConsumer(ReportingDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<DealCreated> context) =>
        await HandleAsync(context.Message);

    public async Task HandleAsync(DealCreated e)
    {
        var existing = await _db.DealSnapshots.FindAsync(e.DealId);
        if (existing != null) return; // idempotent

        _db.DealSnapshots.Add(new DealSnapshot
        {
            DealId = e.DealId,
            Stage = e.Stage.ToString(),
            Value = e.Value,
            UpdatedAt = DateTime.UtcNow
        });

        var proj = await _db.PipelineProjections.FindAsync(e.Stage.ToString());
        if (proj == null)
        {
            proj = new PipelineProjection { Stage = e.Stage.ToString() };
            _db.PipelineProjections.Add(proj);
        }
        proj.DealCount++;
        proj.TotalValue += e.Value;
        proj.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }
}
