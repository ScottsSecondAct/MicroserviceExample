using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ReportingService.Controllers;
using ReportingService.Data;
using ReportingService.Models;

namespace ReportingService.Tests.Controllers;

public class ReportsControllerTests
{
    private ReportingDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ReportingDbContext(options);
    }

    private ReportsController CreateController(ReportingDbContext db) =>
        new(db, NullLogger<ReportsController>.Instance);

    [Fact]
    public async Task GetPipeline_ReturnsOk_WithAllSeededStages()
    {
        using var db = CreateDb();
        db.PipelineProjections.AddRange(
            new PipelineProjection { Stage = "Prospecting", DealCount = 2, TotalValue = 15000 },
            new PipelineProjection { Stage = "Proposal", DealCount = 1, TotalValue = 8000 },
            new PipelineProjection { Stage = "Negotiation", DealCount = 0, TotalValue = 0 },
            new PipelineProjection { Stage = "ClosedWon", DealCount = 3, TotalValue = 45000 },
            new PipelineProjection { Stage = "ClosedLost", DealCount = 1, TotalValue = 5000 });
        await db.SaveChangesAsync();

        var result = await CreateController(db).GetPipeline();

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        var list = ok.Value as IEnumerable<PipelineProjection>;
        list!.Count().Should().Be(5);
    }

    [Fact]
    public async Task GetPipeline_ReturnsOk_WhenEmpty()
    {
        using var db = CreateDb();

        var result = await CreateController(db).GetPipeline();

        var ok = result as OkObjectResult;
        ok!.StatusCode.Should().Be(200);
        var list = ok.Value as IEnumerable<PipelineProjection>;
        list!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActivities_ReturnsOk_OrderedByCountDescending()
    {
        using var db = CreateDb();
        var highRep = Guid.NewGuid();
        var lowRep = Guid.NewGuid();
        db.ActivityRepProjections.AddRange(
            new ActivityRepProjection { OwnerId = lowRep, TotalCount = 2 },
            new ActivityRepProjection { OwnerId = highRep, TotalCount = 10 });
        await db.SaveChangesAsync();

        var result = await CreateController(db).GetActivities();

        var ok = result as OkObjectResult;
        ok!.StatusCode.Should().Be(200);
        var list = (ok.Value as IEnumerable<ActivityRepProjection>)!.ToList();
        list.Count.Should().Be(2);
        list[0].OwnerId.Should().Be(highRep);
        list[1].OwnerId.Should().Be(lowRep);
    }

    [Fact]
    public async Task GetActivities_ReturnsOk_WhenEmpty()
    {
        using var db = CreateDb();

        var result = await CreateController(db).GetActivities();

        var ok = result as OkObjectResult;
        ok!.StatusCode.Should().Be(200);
        (ok.Value as IEnumerable<ActivityRepProjection>)!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetContacts_ReturnsOk_WithAllSeededStatuses()
    {
        using var db = CreateDb();
        db.ContactFunnelProjections.AddRange(
            new ContactFunnelProjection { Status = "Lead", Count = 10 },
            new ContactFunnelProjection { Status = "Prospect", Count = 5 },
            new ContactFunnelProjection { Status = "Customer", Count = 3 },
            new ContactFunnelProjection { Status = "Churned", Count = 1 });
        await db.SaveChangesAsync();

        var result = await CreateController(db).GetContacts();

        var ok = result as OkObjectResult;
        ok!.StatusCode.Should().Be(200);
        var list = ok.Value as IEnumerable<ContactFunnelProjection>;
        list!.Count().Should().Be(4);
    }

    [Fact]
    public async Task GetDashboard_ReturnsOk_WithAllThreeSections()
    {
        using var db = CreateDb();
        db.PipelineProjections.Add(new PipelineProjection { Stage = "Prospecting", DealCount = 1, TotalValue = 5000 });
        db.ActivityRepProjections.Add(new ActivityRepProjection { OwnerId = Guid.NewGuid(), TotalCount = 4 });
        db.ContactFunnelProjections.Add(new ContactFunnelProjection { Status = "Lead", Count = 7 });
        await db.SaveChangesAsync();

        var result = await CreateController(db).GetDashboard();

        var ok = result as OkObjectResult;
        ok!.StatusCode.Should().Be(200);
        // Verify all three projection lists are present using anonymous type reflection
        var value = ok.Value!;
        var type = value.GetType();
        type.GetProperty("pipeline").Should().NotBeNull();
        type.GetProperty("activities").Should().NotBeNull();
        type.GetProperty("contacts").Should().NotBeNull();
    }

    // ── Exception paths ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetPipeline_DbThrows_Returns500()
    {
        var db = CreateDb();
        var controller = CreateController(db);
        db.Dispose();

        var result = await controller.GetPipeline();

        var obj = result as ObjectResult;
        obj!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetActivities_DbThrows_Returns500()
    {
        var db = CreateDb();
        var controller = CreateController(db);
        db.Dispose();

        var result = await controller.GetActivities();

        var obj = result as ObjectResult;
        obj!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetContacts_DbThrows_Returns500()
    {
        var db = CreateDb();
        var controller = CreateController(db);
        db.Dispose();

        var result = await controller.GetContacts();

        var obj = result as ObjectResult;
        obj!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetDashboard_DbThrows_Returns500()
    {
        var db = CreateDb();
        var controller = CreateController(db);
        db.Dispose();

        var result = await controller.GetDashboard();

        var obj = result as ObjectResult;
        obj!.StatusCode.Should().Be(500);
    }
}
