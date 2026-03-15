using FluentAssertions;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Activities.Enums;
using SharedLibrary.Activities.Events;
using SharedLibrary.Contacts.Enums;
using SharedLibrary.Contacts.Events;
using SharedLibrary.Deals.Enums;
using SharedLibrary.Deals.Events;
using System.Net;
using System.Text.Json;

namespace ReportingService.IntegrationTests;

public class ReportsIntegrationTests : IClassFixture<ReportingServiceFactory>
{
    private readonly HttpClient _client;
    private readonly ITestHarness _harness;

    public ReportsIntegrationTests(ReportingServiceFactory factory)
    {
        _client = factory.CreateClient();
        _harness = factory.Services.GetRequiredService<ITestHarness>();
    }

    [Fact]
    public async Task GET_pipeline_Returns200_WithAllFiveStages()
    {
        var response = await _client.GetAsync("/api/reports/pipeline");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        arr.ValueKind.Should().Be(JsonValueKind.Array);
        arr.GetArrayLength().Should().Be(5);
        var stages = arr.EnumerateArray().Select(s => s.GetProperty("stage").GetString()).ToList();
        stages.Should().Contain(new[] { "Prospecting", "Proposal", "Negotiation", "ClosedWon", "ClosedLost" });
    }

    [Fact]
    public async Task GET_contacts_Returns200_WithAllFourStatuses()
    {
        var response = await _client.GetAsync("/api/reports/contacts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        arr.ValueKind.Should().Be(JsonValueKind.Array);
        arr.GetArrayLength().Should().Be(4);
        var statuses = arr.EnumerateArray().Select(s => s.GetProperty("status").GetString()).ToList();
        statuses.Should().Contain(new[] { "Lead", "Prospect", "Customer", "Churned" });
    }

    [Fact]
    public async Task GET_activities_Returns200_WithArray()
    {
        var response = await _client.GetAsync("/api/reports/activities");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        arr.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GET_dashboard_Returns200_WithAllThreeProjections()
    {
        var response = await _client.GetAsync("/api/reports/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        doc.GetProperty("pipeline").ValueKind.Should().Be(JsonValueKind.Array);
        doc.GetProperty("activities").ValueKind.Should().Be(JsonValueKind.Array);
        doc.GetProperty("contacts").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task Consumer_DealCreated_UpdatesPipelineProjection()
    {
        var dealId = Guid.NewGuid();
        await _harness.Bus.Publish(new DealCreated
        {
            DealId = dealId,
            Title = "Test Deal",
            Stage = DealStage.Proposal,
            Value = 12000
        });

        await Task.Delay(500);

        var response = await _client.GetAsync("/api/reports/pipeline");
        var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var proposal = arr.EnumerateArray().First(s => s.GetProperty("stage").GetString() == "Proposal");
        proposal.GetProperty("dealCount").GetInt32().Should().BeGreaterThan(0);
        proposal.GetProperty("totalValue").GetDecimal().Should().BeGreaterThanOrEqualTo(12000);
    }

    [Fact]
    public async Task Consumer_DealStageChanged_MovesDealBetweenStages()
    {
        var dealId = Guid.NewGuid();
        await _harness.Bus.Publish(new DealCreated { DealId = dealId, Stage = DealStage.Prospecting, Value = 7500 });
        await Task.Delay(300);

        await _harness.Bus.Publish(new DealStageChanged
        {
            DealId = dealId,
            OldStage = DealStage.Prospecting,
            NewStage = DealStage.Negotiation
        });
        await Task.Delay(500);

        var response = await _client.GetAsync("/api/reports/pipeline");
        var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        var negotiation = arr.EnumerateArray().First(s => s.GetProperty("stage").GetString() == "Negotiation");
        negotiation.GetProperty("totalValue").GetDecimal().Should().BeGreaterThanOrEqualTo(7500);
    }

    [Fact]
    public async Task Consumer_ActivityLogged_IncrementsRepCount()
    {
        var ownerId = Guid.NewGuid();
        await _harness.Bus.Publish(new ActivityLogged
        {
            ActivityId = Guid.NewGuid(),
            Type = ActivityType.Call,
            Subject = "Rep call",
            OwnerId = ownerId
        });

        await Task.Delay(500);

        var response = await _client.GetAsync("/api/reports/activities");
        var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var rep = arr.EnumerateArray().FirstOrDefault(a =>
            a.GetProperty("ownerId").GetGuid() == ownerId);
        rep.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        rep.GetProperty("totalCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Consumer_ContactStatusChanged_UpdatesFunnel()
    {
        await _harness.Bus.Publish(new ContactStatusChanged
        {
            ContactId = Guid.NewGuid(),
            OldStatus = ContactStatus.Lead,
            NewStatus = ContactStatus.Prospect
        });

        await Task.Delay(500);

        var response = await _client.GetAsync("/api/reports/contacts");
        var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        var prospect = arr.EnumerateArray().First(c => c.GetProperty("status").GetString() == "Prospect");
        prospect.GetProperty("count").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Consumer_DealClosed_IsNoOp_PipelineUnchanged()
    {
        // Seed a deal into Prospecting via DealCreated so we have a baseline
        var dealId = Guid.NewGuid();
        await _harness.Bus.Publish(new DealCreated { DealId = dealId, Stage = DealStage.Prospecting, Value = 3000 });
        await Task.Delay(300);

        var before = await _client.GetAsync("/api/reports/pipeline");
        var beforeArr = JsonDocument.Parse(await before.Content.ReadAsStringAsync()).RootElement;
        var prospectingBefore = beforeArr.EnumerateArray()
            .First(s => s.GetProperty("stage").GetString() == "Prospecting");
        var valueBefore = prospectingBefore.GetProperty("totalValue").GetDecimal();

        // Publish DealClosed — should be a no-op (no double-counting)
        await _harness.Bus.Publish(new DealClosed { DealId = dealId, Stage = DealStage.Prospecting, Value = 3000 });
        await Task.Delay(500);

        var after = await _client.GetAsync("/api/reports/pipeline");
        var afterArr = JsonDocument.Parse(await after.Content.ReadAsStringAsync()).RootElement;
        var prospectingAfter = afterArr.EnumerateArray()
            .First(s => s.GetProperty("stage").GetString() == "Prospecting");
        prospectingAfter.GetProperty("totalValue").GetDecimal().Should().Be(valueBefore);
    }

    [Fact]
    public async Task GET_health_Returns200_Healthy()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
