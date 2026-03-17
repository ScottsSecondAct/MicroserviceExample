using MassTransit;
using ReportingService.Data;
using ReportingService.Models;
using SharedLibrary.Contacts.Events;

namespace ReportingService.Consumers;

public class ContactCreatedConsumer : IConsumer<ContactCreated>
{
    private readonly ReportingDbContext _db;

    public ContactCreatedConsumer(ReportingDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<ContactCreated> context) =>
        await HandleAsync(context.Message);

    public async Task HandleAsync(ContactCreated e)
    {
        const string leadStatus = "Lead";

        var proj = await _db.ContactFunnelProjections.FindAsync(leadStatus);
        if (proj == null)
        {
            proj = new ContactFunnelProjection { Status = leadStatus };
            _db.ContactFunnelProjections.Add(proj);
        }
        proj.Count++;
        proj.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }
}
