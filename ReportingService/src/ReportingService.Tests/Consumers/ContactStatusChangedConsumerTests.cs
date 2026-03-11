using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ReportingService.Consumers;
using ReportingService.Data;
using ReportingService.Models;
using SharedLibrary.Contacts.Enums;
using SharedLibrary.Contacts.Events;

namespace ReportingService.Tests.Consumers;

public class ContactStatusChangedConsumerTests
{
    private ReportingDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ReportingDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_DecrementsOldStatus_IncrementsNewStatus()
    {
        using var db = CreateDb();
        db.ContactFunnelProjections.Add(new ContactFunnelProjection { Status = "Lead", Count = 3 });
        db.ContactFunnelProjections.Add(new ContactFunnelProjection { Status = "Prospect", Count = 1 });
        await db.SaveChangesAsync();

        var consumer = new ContactStatusChangedConsumer(db);
        await consumer.HandleAsync(new ContactStatusChanged
        {
            ContactId = Guid.NewGuid(),
            OldStatus = ContactStatus.Lead,
            NewStatus = ContactStatus.Prospect
        });

        var lead = await db.ContactFunnelProjections.FindAsync("Lead");
        lead!.Count.Should().Be(2);

        var prospect = await db.ContactFunnelProjections.FindAsync("Prospect");
        prospect!.Count.Should().Be(2);
    }

    [Fact]
    public async Task HandleAsync_NewStatusNotSeeded_CreatesProjection()
    {
        using var db = CreateDb();
        db.ContactFunnelProjections.Add(new ContactFunnelProjection { Status = "Lead", Count = 1 });
        await db.SaveChangesAsync();

        var consumer = new ContactStatusChangedConsumer(db);
        await consumer.HandleAsync(new ContactStatusChanged
        {
            ContactId = Guid.NewGuid(),
            OldStatus = ContactStatus.Lead,
            NewStatus = ContactStatus.Customer
        });

        var customer = await db.ContactFunnelProjections.FindAsync("Customer");
        customer.Should().NotBeNull();
        customer!.Count.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_CountNeverGoesBelowZero()
    {
        using var db = CreateDb();
        db.ContactFunnelProjections.Add(new ContactFunnelProjection { Status = "Lead", Count = 0 });
        await db.SaveChangesAsync();

        var consumer = new ContactStatusChangedConsumer(db);
        await consumer.HandleAsync(new ContactStatusChanged
        {
            ContactId = Guid.NewGuid(),
            OldStatus = ContactStatus.Lead,
            NewStatus = ContactStatus.Prospect
        });

        var lead = await db.ContactFunnelProjections.FindAsync("Lead");
        lead!.Count.Should().Be(0);
    }
}
