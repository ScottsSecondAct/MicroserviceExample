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

    [Fact]
    public async Task GetAccount_ById_ReturnsAccount()
    {
        await LoginAsync();

        var createResponse = await _client.PostAsync("/accounts/api/accounts",
            new { name = "Lookup Corp", industry = "Healthcare", size = "Small" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var accountId = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("accountId").GetGuid();

        var getResponse = await _client.GetAsync($"/accounts/api/accounts/{accountId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var account = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        account.RootElement.GetProperty("accountId").GetGuid().Should().Be(accountId);
        account.RootElement.GetProperty("name").GetString().Should().Be("Lookup Corp");
    }

    [Fact]
    public async Task ListAccounts_IncludesCreatedAccount()
    {
        await LoginAsync();

        var createResponse = await _client.PostAsync("/accounts/api/accounts",
            new { name = $"List Corp {Guid.NewGuid()}", industry = "Retail", size = "Medium" });
        var accountId = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("accountId").GetGuid();

        var listResponse = await _client.GetAsync("/accounts/api/accounts");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var accounts = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync()).RootElement;
        accounts.ValueKind.Should().Be(JsonValueKind.Array);
        accounts.EnumerateArray().Should().Contain(a => a.GetProperty("accountId").GetGuid() == accountId);
    }

    [Fact]
    public async Task UpdateAccount_ChangesNameAndIndustry()
    {
        await LoginAsync();

        var createResponse = await _client.PostAsync("/accounts/api/accounts",
            new { name = "Old Name", industry = "Finance", size = "Large" });
        var accountId = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("accountId").GetGuid();

        var updateResponse = await _client.PutAsync($"/accounts/api/accounts/{accountId}",
            new { name = "New Name", industry = "Technology", size = "Small" });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/accounts/api/accounts/{accountId}");
        var account = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        account.RootElement.GetProperty("name").GetString().Should().Be("New Name");
        account.RootElement.GetProperty("industry").GetString().Should().Be("Technology");
    }

    [Fact]
    public async Task DeleteContact_RemovesDealContactAssociation()
    {
        await LoginAsync();

        // Create a contact
        var contactResponse = await _client.PostAsync("/contacts/api/contacts",
            new { firstName = "Del", lastName = "Contact", email = $"del-{Guid.NewGuid()}@example.com" });
        var contactId = JsonDocument.Parse(await contactResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("contactId").GetGuid();

        // Create a deal and associate the contact
        var dealResponse = await _client.PostAsync("/deals/api/deals",
            new { title = "Deal For Contact Deletion", value = 5000 });
        var dealId = JsonDocument.Parse(await dealResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("dealId").GetGuid();

        await _client.PostAsync($"/deals/api/deals/{dealId}/contacts",
            new { contactId, role = "Champion" });

        // Verify contact is on the deal
        var dealBefore = JsonDocument.Parse(
            await (await _client.GetAsync($"/deals/api/deals/{dealId}")).Content.ReadAsStringAsync());
        dealBefore.RootElement.GetProperty("contacts").GetArrayLength().Should().Be(1);

        // Delete the contact — fires ContactDeleted event
        var deleteResponse = await _client.DeleteAsync($"/contacts/api/contacts/{contactId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Poll until DealService processes the ContactDeleted event and removes the association
        await RetryHelper.WaitUntilAsync(async () =>
        {
            var r = await _client.GetAsync($"/deals/api/deals/{dealId}");
            var deal = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
            return deal.RootElement.GetProperty("contacts").GetArrayLength() == 0;
        }, timeout: TimeSpan.FromSeconds(15));

        var dealAfter = JsonDocument.Parse(
            await (await _client.GetAsync($"/deals/api/deals/{dealId}")).Content.ReadAsStringAsync());
        dealAfter.RootElement.GetProperty("contacts").GetArrayLength().Should().Be(0);
    }

    public void Dispose() => _client.Dispose();
}
