using System.Net;
using System.Text.Json;
using EndToEnd.Tests.Infrastructure;
using FluentAssertions;

namespace EndToEnd.Tests.Flows;

public class AccountContactFlowTests : IDisposable
{
    private readonly GatewayClient _client = new();

    private Task LoginAsync() => _client.LoginAsAdminAsync();

    [Fact]
    public async Task FullLifecycle_CreateUpdateDelete()
    {
        await LoginAsync();

        // Create account
        var accountResponse = await _client.PostAsync("/accounts/api/accounts",
            new { name = "E2E Corp", industry = "Technology", size = "Medium" });
        accountResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var accountId = JsonDocument.Parse(await accountResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("accountId").GetGuid();

        // Create contact associated with the account
        var contactResponse = await _client.PostAsync("/contacts/api/contacts",
            new { firstName = "Jane", lastName = "Doe", email = $"jane-{Guid.NewGuid()}@example.com", accountId });
        contactResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var contactId = JsonDocument.Parse(await contactResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("contactId").GetGuid();

        // Verify contact appears when filtering by accountId
        var filteredResponse = await _client.GetAsync($"/contacts/api/contacts?accountId={accountId}");
        filteredResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var contacts = JsonDocument.Parse(await filteredResponse.Content.ReadAsStringAsync()).RootElement;
        contacts.ValueKind.Should().Be(JsonValueKind.Array);
        contacts.EnumerateArray().Should().Contain(c =>
            c.GetProperty("contactId").GetGuid() == contactId);

        // Transition contact status to Prospect
        var updateResponse = await _client.PutAsync($"/contacts/api/contacts/{contactId}",
            new { status = "Prospect" });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify status updated
        var getResponse = await _client.GetAsync($"/contacts/api/contacts/{contactId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        updated.RootElement.GetProperty("status").GetString().Should().Be("Prospect");

        // Delete account
        var deleteResponse = await _client.DeleteAsync($"/accounts/api/accounts/{accountId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateContact_WithInvalidAccountId_Returns400()
    {
        await LoginAsync();

        var response = await _client.PostAsync("/contacts/api/contacts",
            new
            {
                firstName = "Ghost",
                lastName = "User",
                email = $"ghost-{Guid.NewGuid()}@example.com",
                accountId = Guid.NewGuid()
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    public void Dispose() => _client.Dispose();
}
