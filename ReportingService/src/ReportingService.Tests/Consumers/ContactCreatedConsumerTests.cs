using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;
using ReportingService.Consumers;
using ReportingService.Data;
using ReportingService.Models;
using SharedLibrary.Contacts.Events;

namespace ReportingService.Tests.Consumers;

public class ContactCreatedConsumerTests
{
    private ReportingDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ReportingDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_IncrementsLeadCount_WhenLeadAlreadySeeded()
    {
        using var db = CreateDb();
        db.ContactFunnelProjections.Add(new ContactFunnelProjection { Status = "Lead", Count = 2 });
        await db.SaveChangesAsync();

        var consumer = new ContactCreatedConsumer(db);
        await consumer.HandleAsync(new ContactCreated
        {
            ContactId = Guid.NewGuid(),
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com"
        });

        var lead = await db.ContactFunnelProjections.FindAsync("Lead");
        lead!.Count.Should().Be(3);
    }

    [Fact]
    public async Task HandleAsync_CreatesLeadProjection_WhenNotSeeded()
    {
        using var db = CreateDb();

        var consumer = new ContactCreatedConsumer(db);
        await consumer.HandleAsync(new ContactCreated
        {
            ContactId = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Smith",
            Email = "john@example.com"
        });

        var lead = await db.ContactFunnelProjections.FindAsync("Lead");
        lead.Should().NotBeNull();
        lead!.Count.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_DoesNotAffectOtherStatuses()
    {
        using var db = CreateDb();
        db.ContactFunnelProjections.Add(new ContactFunnelProjection { Status = "Lead", Count = 1 });
        db.ContactFunnelProjections.Add(new ContactFunnelProjection { Status = "Prospect", Count = 5 });
        await db.SaveChangesAsync();

        var consumer = new ContactCreatedConsumer(db);
        await consumer.HandleAsync(new ContactCreated
        {
            ContactId = Guid.NewGuid(),
            FirstName = "Alice",
            LastName = "Jones",
            Email = "alice@example.com"
        });

        var prospect = await db.ContactFunnelProjections.FindAsync("Prospect");
        prospect!.Count.Should().Be(5);
    }

    [Fact]
    public async Task Consume_DelegatesToHandleAsync()
    {
        using var db = CreateDb();
        var consumer = new ContactCreatedConsumer(db);
        var message = new ContactCreated
        {
            ContactId = Guid.NewGuid(),
            FirstName = "Bob",
            LastName = "Brown",
            Email = "bob@example.com"
        };
        var contextMock = new Mock<ConsumeContext<ContactCreated>>();
        contextMock.Setup(c => c.Message).Returns(message);

        await consumer.Consume(contextMock.Object);

        var lead = await db.ContactFunnelProjections.FindAsync("Lead");
        lead!.Count.Should().Be(1);
    }
}
