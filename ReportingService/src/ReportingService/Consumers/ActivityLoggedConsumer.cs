using MassTransit;
using ReportingService.Data;
using ReportingService.Models;
using SharedLibrary.Activities.Events;

namespace ReportingService.Consumers;

public class ActivityLoggedConsumer : IConsumer<ActivityLogged>
{
    private readonly ReportingDbContext _db;

    public ActivityLoggedConsumer(ReportingDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<ActivityLogged> context) =>
        await HandleAsync(context.Message);

    public async Task HandleAsync(ActivityLogged e)
    {
        if (e.OwnerId == null) return;

        var proj = await _db.ActivityRepProjections.FindAsync(e.OwnerId.Value);
        if (proj == null)
        {
            proj = new ActivityRepProjection { OwnerId = e.OwnerId.Value };
            _db.ActivityRepProjections.Add(proj);
        }
        proj.TotalCount++;
        proj.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }
}
