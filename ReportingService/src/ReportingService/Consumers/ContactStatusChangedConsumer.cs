using MassTransit;
using ReportingService.Data;
using ReportingService.Models;
using SharedLibrary.Contacts.Events;

namespace ReportingService.Consumers;

public class ContactStatusChangedConsumer : IConsumer<ContactStatusChanged>
{
    private readonly ReportingDbContext _db;

    public ContactStatusChangedConsumer(ReportingDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<ContactStatusChanged> context) =>
        await HandleAsync(context.Message);

    public async Task HandleAsync(ContactStatusChanged e)
    {
        var oldStatus = e.OldStatus.ToString();
        var newStatus = e.NewStatus.ToString();

        var oldProj = await _db.ContactFunnelProjections.FindAsync(oldStatus);
        if (oldProj != null)
        {
            oldProj.Count = Math.Max(0, oldProj.Count - 1);
            oldProj.UpdatedAt = DateTime.UtcNow;
        }

        var newProj = await _db.ContactFunnelProjections.FindAsync(newStatus);
        if (newProj == null)
        {
            newProj = new ContactFunnelProjection { Status = newStatus };
            _db.ContactFunnelProjections.Add(newProj);
        }
        newProj.Count++;
        newProj.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }
}
