using FluentAssertions;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Contacts.Events;
using SharedLibrary.Deals.Events;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace DealService.IntegrationTests;

public class DealsIntegrationTests : IClassFixture<DealServiceFactory>
{
  private readonly HttpClient _client;
  private readonly ITestHarness _harness;
  private readonly DealServiceFactory _factory;

  public DealsIntegrationTests(DealServiceFactory factory)
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

  private void StubContact(Guid contactId, bool exists)
  {
    _factory.ContactServiceMock
      .Given(Request.Create().WithPath($"/api/contacts/{contactId}").UsingGet())
      .RespondWith(exists
        ? Response.Create().WithStatusCode(200).WithBody("{\"contactId\":\"" + contactId + "\"}")
        : Response.Create().WithStatusCode(404));
  }

  [Fact]
  public async Task POST_deals_Returns201_And_PublishesDealCreated()
  {
    var response = await _client.PostAsJsonAsync("/api/deals",
      new { title = "New Deal", value = 10000, stage = "Prospecting" });

    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var body = await response.Content.ReadAsStringAsync();
    var doc = JsonDocument.Parse(body);
    doc.RootElement.GetProperty("title").GetString().Should().Be("New Deal");
    var published = await _harness.Published.SelectAsync<DealCreated>().Any();
    published.Should().BeTrue();
  }

  [Fact]
  public async Task POST_deals_WithInvalidAccountId_Returns400_NoEvent()
  {
    var accountId = Guid.NewGuid();
    StubAccount(accountId, false);
    var countBefore = _harness.Published.SelectAsync<DealCreated>().ToBlockingEnumerable().Count();

    var response = await _client.PostAsJsonAsync("/api/deals",
      new { title = "Bad Account Deal", accountId, value = 5000 });

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var countAfter = _harness.Published.SelectAsync<DealCreated>().ToBlockingEnumerable().Count();
    countAfter.Should().Be(countBefore);
  }

  [Fact]
  public async Task POST_deals_WithMissingTitle_Returns400()
  {
    var response = await _client.PostAsJsonAsync("/api/deals", new { title = "", value = 100 });

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task GET_deals_Returns200_WithList()
  {
    await _client.PostAsJsonAsync("/api/deals", new { title = "Listed Deal", value = 0 });

    var response = await _client.GetAsync("/api/deals");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    arr.ValueKind.Should().Be(JsonValueKind.Array);
  }

  [Fact]
  public async Task GET_deals_ById_Returns200_WithAllFields()
  {
    var created = await _client.PostAsJsonAsync("/api/deals",
      new { title = "Detail Deal", value = 9999, probability = 75 });
    var dealId = JsonDocument.Parse(await created.Content.ReadAsStringAsync())
      .RootElement.GetProperty("dealId").GetGuid();

    var response = await _client.GetAsync($"/api/deals/{dealId}");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    doc.RootElement.GetProperty("dealId").GetGuid().Should().Be(dealId);
    doc.RootElement.GetProperty("title").GetString().Should().Be("Detail Deal");
  }

  [Fact]
  public async Task GET_deals_ById_Returns404_WhenMissing()
  {
    var response = await _client.GetAsync($"/api/deals/{Guid.NewGuid()}");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task PUT_deals_StageChange_Returns200_And_PublishesDealStageChanged()
  {
    var created = await _client.PostAsJsonAsync("/api/deals",
      new { title = "Stage Deal", value = 1000, stage = "Prospecting" });
    var dealId = JsonDocument.Parse(await created.Content.ReadAsStringAsync())
      .RootElement.GetProperty("dealId").GetGuid();

    var response = await _client.PutAsJsonAsync($"/api/deals/{dealId}", new { stage = "Proposal" });

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var published = await _harness.Published.SelectAsync<DealStageChanged>().Any();
    published.Should().BeTrue();
  }

  [Fact]
  public async Task PUT_deals_ClosedWon_PublishesDealStageChanged_And_DealClosed()
  {
    var created = await _client.PostAsJsonAsync("/api/deals",
      new { title = "Close Deal", value = 20000, stage = "Negotiation" });
    var dealId = JsonDocument.Parse(await created.Content.ReadAsStringAsync())
      .RootElement.GetProperty("dealId").GetGuid();

    var response = await _client.PutAsJsonAsync($"/api/deals/{dealId}", new { stage = "ClosedWon" });

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    (await _harness.Published.SelectAsync<DealStageChanged>().Any()).Should().BeTrue();
    (await _harness.Published.SelectAsync<DealClosed>().Any()).Should().BeTrue();
  }

  [Fact]
  public async Task DELETE_deals_Returns204()
  {
    var created = await _client.PostAsJsonAsync("/api/deals", new { title = "Delete Me", value = 0 });
    var dealId = JsonDocument.Parse(await created.Content.ReadAsStringAsync())
      .RootElement.GetProperty("dealId").GetGuid();

    var response = await _client.DeleteAsync($"/api/deals/{dealId}");

    response.StatusCode.Should().Be(HttpStatusCode.NoContent);
  }

  [Fact]
  public async Task GET_pipeline_Returns200_WithStages()
  {
    var response = await _client.GetAsync("/api/pipeline");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    arr.ValueKind.Should().Be(JsonValueKind.Array);
    arr.GetArrayLength().Should().Be(5); // Prospecting, Proposal, Negotiation, ClosedWon, ClosedLost
  }

  [Fact]
  public async Task Consumer_ContactDeleted_RemovesDealContactAssociations()
  {
    var contactId = Guid.NewGuid();
    StubContact(contactId, true);
    var created = await _client.PostAsJsonAsync("/api/deals", new { title = "Contact Deal", value = 0 });
    var dealId = JsonDocument.Parse(await created.Content.ReadAsStringAsync())
      .RootElement.GetProperty("dealId").GetGuid();
    await _client.PostAsJsonAsync($"/api/deals/{dealId}/contacts",
      new { contactId, role = "Champion" });

    await _harness.Bus.Publish(new ContactDeleted { ContactId = contactId });

    await Task.Delay(500);

    var dealResponse = await _client.GetAsync($"/api/deals/{dealId}");
    var doc = JsonDocument.Parse(await dealResponse.Content.ReadAsStringAsync());
    doc.RootElement.GetProperty("contacts").GetArrayLength().Should().Be(0);
  }

  [Fact]
  public async Task GET_health_Returns200_Healthy()
  {
    var response = await _client.GetAsync("/health");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
  }
}
