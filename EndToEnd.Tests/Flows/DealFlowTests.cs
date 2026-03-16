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

    public void Dispose() => _client.Dispose();
}
