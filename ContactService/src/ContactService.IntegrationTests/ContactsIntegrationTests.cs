using FluentAssertions;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Contacts.Events;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace ContactService.IntegrationTests;

public class ContactsIntegrationTests : IClassFixture<ContactServiceFactory>
{
  private readonly HttpClient _client;
  private readonly ITestHarness _harness;
  private readonly ContactServiceFactory _factory;

  public ContactsIntegrationTests(ContactServiceFactory factory)
  {
    _factory = factory;
    _client = factory.CreateClient();
    _harness = factory.Services.GetRequiredService<ITestHarness>();
  }

  private void StubAccount(Guid accountId, bool exists)
  {
    _factory.AccountServiceMock
      .Given(Request.Create().WithPath($"/api/accounts/{accountId}").UsingGet())
      .RespondWith(exists
        ? Response.Create().WithStatusCode(200).WithBody("{\"accountId\":\"" + accountId + "\"}")
        : Response.Create().WithStatusCode(404));
  }

  private static object CreateContactPayload(string first = "Jane", string last = "Doe", string email = null!, Guid? accountId = null)
  {
    email ??= $"{first.ToLower()}.{last.ToLower()}@example.com";
    return new { firstName = first, lastName = last, email, accountId };
  }

  [Fact]
  public async Task POST_contacts_WithValidAccountId_Returns201_And_PublishesContactCreated()
  {
    var accountId = Guid.NewGuid();
    StubAccount(accountId, true);

    var response = await _client.PostAsJsonAsync("/api/contacts", CreateContactPayload(accountId: accountId));

    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var published = await _harness.Published.SelectAsync<ContactCreated>().Any();
    published.Should().BeTrue();
  }

  [Fact]
  public async Task POST_contacts_WithInvalidAccountId_Returns400_NoEvent()
  {
    var accountId = Guid.NewGuid();
    StubAccount(accountId, false);
    var countBefore = _harness.Published.SelectAsync<ContactCreated>().ToBlockingEnumerable().Count();

    var response = await _client.PostAsJsonAsync("/api/contacts", CreateContactPayload(accountId: accountId));

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var countAfter = _harness.Published.SelectAsync<ContactCreated>().ToBlockingEnumerable().Count();
    countAfter.Should().Be(countBefore);
  }

  [Fact]
  public async Task POST_contacts_WithNoAccountId_Returns201()
  {
    var response = await _client.PostAsJsonAsync("/api/contacts",
      new { firstName = "NoAccount", lastName = "User", email = "no@account.com" });

    response.StatusCode.Should().Be(HttpStatusCode.Created);
  }

  [Fact]
  public async Task PUT_contacts_StatusChange_Returns200_And_PublishesContactStatusChanged()
  {
    var created = await _client.PostAsJsonAsync("/api/contacts",
      new { firstName = "Status", lastName = "Change", email = "status@change.com", status = "Lead" });
    var contactId = JsonDocument.Parse(await created.Content.ReadAsStringAsync())
      .RootElement.GetProperty("contactId").GetGuid();

    var response = await _client.PutAsJsonAsync($"/api/contacts/{contactId}", new { status = "Prospect" });

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var published = await _harness.Published.SelectAsync<ContactStatusChanged>().Any();
    published.Should().BeTrue();
  }

  [Fact]
  public async Task DELETE_contacts_Returns204_And_PublishesContactDeleted()
  {
    var created = await _client.PostAsJsonAsync("/api/contacts",
      new { firstName = "To", lastName = "Delete", email = "to@delete.com" });
    var contactId = JsonDocument.Parse(await created.Content.ReadAsStringAsync())
      .RootElement.GetProperty("contactId").GetGuid();

    var response = await _client.DeleteAsync($"/api/contacts/{contactId}");

    response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    var published = await _harness.Published.SelectAsync<ContactDeleted>().Any();
    published.Should().BeTrue();
  }

  [Fact]
  public async Task GET_contacts_Returns200_FullList()
  {
    await _client.PostAsJsonAsync("/api/contacts", CreateContactPayload("List", "User", "list@user.com"));

    var response = await _client.GetAsync("/api/contacts");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    arr.ValueKind.Should().Be(JsonValueKind.Array);
  }

  [Fact]
  public async Task GET_contacts_StatusFilter_ReturnsFilteredList()
  {
    await _client.PostAsJsonAsync("/api/contacts",
      new { firstName = "Lead", lastName = "User", email = "lead@filter.com", status = "Lead" });

    var response = await _client.GetAsync("/api/contacts?status=Lead");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    foreach (var item in arr.EnumerateArray())
      item.GetProperty("status").GetString().Should().Be("Lead");
  }

  [Fact]
  public async Task GET_contacts_ById_Returns404_WhenMissing()
  {
    var response = await _client.GetAsync($"/api/contacts/{Guid.NewGuid()}");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task GET_health_Returns200_Healthy()
  {
    var response = await _client.GetAsync("/health");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
  }
}
