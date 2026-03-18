using System.Net;
using System.Text.Json;
using EndToEnd.Tests.Infrastructure;
using FluentAssertions;

namespace EndToEnd.Tests.Flows;

public class ReportingFlowTests : IDisposable
{
    private readonly GatewayClient _client = new();

    private Task LoginAsync() => _client.LoginAsAdminAsync();

    [Fact]
    public async Task GetPipeline_ReturnsAllFiveStages()
    {
        await LoginAsync();

        var response = await _client.GetAsync("/reports/api/reports/pipeline");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        arr.ValueKind.Should().Be(JsonValueKind.Array);
        arr.GetArrayLength().Should().Be(5);
        var stages = arr.EnumerateArray().Select(s => s.GetProperty("stage").GetString()).ToList();
        stages.Should().Contain(new[] { "Prospecting", "Proposal", "Negotiation", "ClosedWon", "ClosedLost" });
    }

    [Fact]
    public async Task GetContacts_ReturnsAllFourStatuses()
    {
        await LoginAsync();

        var response = await _client.GetAsync("/reports/api/reports/contacts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        arr.GetArrayLength().Should().Be(4);
        var statuses = arr.EnumerateArray().Select(s => s.GetProperty("status").GetString()).ToList();
        statuses.Should().Contain(new[] { "Lead", "Prospect", "Customer", "Churned" });
    }

    [Fact]
    public async Task GetDashboard_ReturnsCombinedProjections()
    {
        await LoginAsync();

        var response = await _client.GetAsync("/reports/api/reports/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        doc.GetProperty("pipeline").ValueKind.Should().Be(JsonValueKind.Array);
        doc.GetProperty("activities").ValueKind.Should().Be(JsonValueKind.Array);
        doc.GetProperty("contacts").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task DealCreated_EventuallyAppearsInPipelineProjection()
    {
        await LoginAsync();

        var dealResponse = await _client.PostAsync("/deals/api/deals",
            new { title = "E2E Reporting Deal", value = 25000, stage = "Proposal" });
        dealResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Poll until the ReportingService consumer has processed the DealCreated event
        await RetryHelper.WaitUntilAsync(async () =>
        {
            var r = await _client.GetAsync("/reports/api/reports/pipeline");
            var arr = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
            var proposal = arr.EnumerateArray().FirstOrDefault(s => s.GetProperty("stage").GetString() == "Proposal");
            return proposal.ValueKind != JsonValueKind.Undefined &&
                   proposal.GetProperty("totalValue").GetDecimal() >= 25000;
        }, timeout: TimeSpan.FromSeconds(15));

        var response = await _client.GetAsync("/reports/api/reports/pipeline");
        var stages = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var proposalStage = stages.EnumerateArray().First(s => s.GetProperty("stage").GetString() == "Proposal");
        proposalStage.GetProperty("dealCount").GetInt32().Should().BeGreaterThan(0);
        proposalStage.GetProperty("totalValue").GetDecimal().Should().BeGreaterThanOrEqualTo(25000);
    }

    [Fact]
    public async Task ContactStatusChanged_EventuallyAppearsInContactFunnel()
    {
        await LoginAsync();

        var contactResponse = await _client.PostAsync("/contacts/api/contacts",
            new { firstName = "Report", lastName = "Test", email = $"report-{Guid.NewGuid()}@example.com" });
        var contactId = JsonDocument.Parse(await contactResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("contactId").GetGuid();

        await _client.PutAsync($"/contacts/api/contacts/{contactId}", new { status = "Prospect" });

        // Poll until ContactStatusChanged event is processed
        await RetryHelper.WaitUntilAsync(async () =>
        {
            var r = await _client.GetAsync("/reports/api/reports/contacts");
            var arr = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
            var prospect = arr.EnumerateArray().FirstOrDefault(c => c.GetProperty("status").GetString() == "Prospect");
            return prospect.ValueKind != JsonValueKind.Undefined &&
                   prospect.GetProperty("count").GetInt32() > 0;
        }, timeout: TimeSpan.FromSeconds(15));

        var response = await _client.GetAsync("/reports/api/reports/contacts");
        var statuses = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var prospectStatus = statuses.EnumerateArray().First(c => c.GetProperty("status").GetString() == "Prospect");
        prospectStatus.GetProperty("count").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetActivities_ReturnsActivityProjections()
    {
        await LoginAsync();

        var response = await _client.GetAsync("/reports/api/reports/activities");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        arr.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task ReportsRoute_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/reports/api/reports/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public void Dispose() => _client.Dispose();
}
