using System.Net;
using System.Text.Json;
using EndToEnd.Tests.Infrastructure;
using FluentAssertions;

namespace EndToEnd.Tests.Flows;

public class DealFlowTests : IDisposable
{
    private readonly GatewayClient _client = new();

    private Task LoginAsync() => _client.LoginAsAdminAsync();

    [Fact]
    public async Task PipelineLifecycle_CreateDeal_AddContact_CloseWon()
    {
        await LoginAsync();

        // Create account and contact for deal associations
        var accountResponse = await _client.PostAsync("/accounts/api/accounts",
            new { name = "Deal Account", industry = "Finance", size = "Large" });
        var accountId = JsonDocument.Parse(await accountResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("accountId").GetGuid();

        var contactResponse = await _client.PostAsync("/contacts/api/contacts",
            new { firstName = "Deal", lastName = "Contact", email = $"deal-{Guid.NewGuid()}@example.com", accountId });
        var contactId = JsonDocument.Parse(await contactResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("contactId").GetGuid();

        // Create deal
        var dealResponse = await _client.PostAsync("/deals/api/deals",
            new { title = "E2E Deal", value = 50000, stage = "Prospecting", accountId });
        dealResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var dealId = JsonDocument.Parse(await dealResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("dealId").GetGuid();

        // Add contact to deal
        var addContactResponse = await _client.PostAsync($"/deals/api/deals/{dealId}/contacts",
            new { contactId, role = "Champion" });
        addContactResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify deal exists with contact
        var getResponse = await _client.GetAsync($"/deals/api/deals/{dealId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deal = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        deal.RootElement.GetProperty("contacts").GetArrayLength().Should().Be(1);

        // Close the deal (ClosedWon)
        var closeResponse = await _client.PutAsync($"/deals/api/deals/{dealId}", new { stage = "ClosedWon" });
        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var closed = JsonDocument.Parse(await closeResponse.Content.ReadAsStringAsync());
        closed.RootElement.GetProperty("stage").GetString().Should().Be("ClosedWon");

        // Verify the pipeline board includes the deal in ClosedWon
        var pipelineResponse = await _client.GetAsync("/pipeline/api/pipeline");
        pipelineResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var stages = JsonDocument.Parse(await pipelineResponse.Content.ReadAsStringAsync()).RootElement;
        stages.ValueKind.Should().Be(JsonValueKind.Array);
        stages.GetArrayLength().Should().Be(5);

        var closedWonStage = stages.EnumerateArray()
            .First(s => s.GetProperty("stage").GetString() == "ClosedWon");
        closedWonStage.GetProperty("deals").EnumerateArray()
            .Should().Contain(d => d.GetProperty("dealId").GetGuid() == dealId);
    }

    [Fact]
    public async Task ListDeals_IncludesCreatedDeal()
    {
        await LoginAsync();

        var dealResponse = await _client.PostAsync("/deals/api/deals",
            new { title = $"Listed Deal {Guid.NewGuid()}", value = 1000 });
        var dealId = JsonDocument.Parse(await dealResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("dealId").GetGuid();

        var listResponse = await _client.GetAsync("/deals/api/deals");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deals = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync()).RootElement;
        deals.ValueKind.Should().Be(JsonValueKind.Array);
        deals.EnumerateArray().Should().Contain(d => d.GetProperty("dealId").GetGuid() == dealId);
    }

    [Fact]
    public async Task DeleteDeal_Returns204_AndDealIsGone()
    {
        await LoginAsync();

        var dealResponse = await _client.PostAsync("/deals/api/deals",
            new { title = "Deal To Delete", value = 500 });
        var dealId = JsonDocument.Parse(await dealResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("dealId").GetGuid();

        var deleteResponse = await _client.DeleteAsync($"/deals/api/deals/{dealId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/deals/api/deals/{dealId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveContactFromDeal_Returns204()
    {
        await LoginAsync();

        var contactResponse = await _client.PostAsync("/contacts/api/contacts",
            new { firstName = "Rem", lastName = "Contact", email = $"rem-{Guid.NewGuid()}@example.com" });
        var contactId = JsonDocument.Parse(await contactResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("contactId").GetGuid();

        var dealResponse = await _client.PostAsync("/deals/api/deals",
            new { title = "Deal With Removable Contact", value = 2000 });
        var dealId = JsonDocument.Parse(await dealResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("dealId").GetGuid();

        await _client.PostAsync($"/deals/api/deals/{dealId}/contacts",
            new { contactId, role = "Influencer" });

        var removeResponse = await _client.DeleteAsync($"/deals/api/deals/{dealId}/contacts/{contactId}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/deals/api/deals/{dealId}");
        var deal = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        deal.RootElement.GetProperty("contacts").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task DealStageChanged_UpdatesReportingPipelineCounts()
    {
        await LoginAsync();

        const int dealValue = 30000;
        var dealResponse = await _client.PostAsync("/deals/api/deals",
            new { title = "Stage Change Deal", value = dealValue, stage = "Prospecting" });
        dealResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var dealId = JsonDocument.Parse(await dealResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("dealId").GetGuid();

        // Wait for DealCreated event to be processed so DealSnapshot exists
        await RetryHelper.WaitUntilAsync(async () =>
        {
            var r = await _client.GetAsync("/reports/api/reports/pipeline");
            var arr = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
            var prospecting = arr.EnumerateArray()
                .FirstOrDefault(s => s.GetProperty("stage").GetString() == "Prospecting");
            return prospecting.ValueKind != JsonValueKind.Undefined &&
                   prospecting.GetProperty("dealCount").GetInt32() >= 1;
        }, timeout: TimeSpan.FromSeconds(15));

        // Move the deal to Negotiation
        var updateResponse = await _client.PutAsync($"/deals/api/deals/{dealId}",
            new { stage = "Negotiation" });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Poll until ReportingService processes DealStageChanged and updates Negotiation counts
        await RetryHelper.WaitUntilAsync(async () =>
        {
            var r = await _client.GetAsync("/reports/api/reports/pipeline");
            var arr = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
            var negotiation = arr.EnumerateArray()
                .FirstOrDefault(s => s.GetProperty("stage").GetString() == "Negotiation");
            return negotiation.ValueKind != JsonValueKind.Undefined &&
                   negotiation.GetProperty("totalValue").GetDecimal() >= dealValue;
        }, timeout: TimeSpan.FromSeconds(15));

        var pipelineResponse = await _client.GetAsync("/reports/api/reports/pipeline");
        var stages = JsonDocument.Parse(await pipelineResponse.Content.ReadAsStringAsync()).RootElement;
        var negotiationStage = stages.EnumerateArray()
            .First(s => s.GetProperty("stage").GetString() == "Negotiation");
        negotiationStage.GetProperty("dealCount").GetInt32().Should().BeGreaterThan(0);
        negotiationStage.GetProperty("totalValue").GetDecimal().Should().BeGreaterThanOrEqualTo(dealValue);
    }

    public void Dispose() => _client.Dispose();
}
