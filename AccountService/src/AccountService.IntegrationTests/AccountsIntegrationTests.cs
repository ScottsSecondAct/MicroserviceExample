using FluentAssertions;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Accounts.Events;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AccountService.IntegrationTests;

public class AccountsIntegrationTests : IClassFixture<AccountServiceFactory>
{
  private readonly HttpClient _client;
  private readonly ITestHarness _harness;

  public AccountsIntegrationTests(AccountServiceFactory factory)
  {
    _client = factory.CreateClient();
    _harness = factory.Services.GetRequiredService<ITestHarness>();
  }

  private static object CreateAccountPayload(string name = "Acme Corp") => new
  {
    name,
    industry = "Technology",
    size = "Medium",
    website = "https://acme.com"
  };

  [Fact]
  public async Task POST_accounts_Returns201_And_PublishesAccountCreated()
  {
    var response = await _client.PostAsJsonAsync("/api/accounts", CreateAccountPayload());

    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var body = await response.Content.ReadAsStringAsync();
    var doc = JsonDocument.Parse(body);
    doc.RootElement.GetProperty("name").GetString().Should().Be("Acme Corp");

    var published = await _harness.Published.SelectAsync<AccountCreated>().Any();
    published.Should().BeTrue();
  }

  [Fact]
  public async Task GET_accounts_Returns200_WithList()
  {
    await _client.PostAsJsonAsync("/api/accounts", CreateAccountPayload("List Corp"));

    var response = await _client.GetAsync("/api/accounts");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var body = await response.Content.ReadAsStringAsync();
    var accounts = JsonDocument.Parse(body).RootElement;
    accounts.ValueKind.Should().Be(JsonValueKind.Array);
  }

  [Fact]
  public async Task GET_accounts_ById_Returns200_WithAllFields()
  {
    var created = await _client.PostAsJsonAsync("/api/accounts", CreateAccountPayload("Detail Corp"));
    var createdBody = await created.Content.ReadAsStringAsync();
    var accountId = JsonDocument.Parse(createdBody).RootElement.GetProperty("accountId").GetGuid();

    var response = await _client.GetAsync($"/api/accounts/{accountId}");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var body = await response.Content.ReadAsStringAsync();
    var doc = JsonDocument.Parse(body);
    doc.RootElement.GetProperty("accountId").GetGuid().Should().Be(accountId);
    doc.RootElement.GetProperty("name").GetString().Should().Be("Detail Corp");
  }

  [Fact]
  public async Task GET_accounts_ById_Returns404_WhenMissing()
  {
    var response = await _client.GetAsync($"/api/accounts/{Guid.NewGuid()}");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task PUT_accounts_Returns200_And_UpdatesDB()
  {
    var created = await _client.PostAsJsonAsync("/api/accounts", CreateAccountPayload("Before Update"));
    var createdBody = await created.Content.ReadAsStringAsync();
    var accountId = JsonDocument.Parse(createdBody).RootElement.GetProperty("accountId").GetGuid();

    var response = await _client.PutAsJsonAsync($"/api/accounts/{accountId}", new { name = "After Update" });

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var body = await response.Content.ReadAsStringAsync();
    JsonDocument.Parse(body).RootElement.GetProperty("name").GetString().Should().Be("After Update");
  }

  [Fact]
  public async Task PUT_accounts_Returns404_WhenMissing()
  {
    var response = await _client.PutAsJsonAsync($"/api/accounts/{Guid.NewGuid()}", new { name = "X" });

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task DELETE_accounts_Returns204_And_PublishesAccountDeleted()
  {
    var created = await _client.PostAsJsonAsync("/api/accounts", CreateAccountPayload("To Delete"));
    var createdBody = await created.Content.ReadAsStringAsync();
    var accountId = JsonDocument.Parse(createdBody).RootElement.GetProperty("accountId").GetGuid();

    var response = await _client.DeleteAsync($"/api/accounts/{accountId}");

    response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    var published = await _harness.Published.SelectAsync<AccountDeleted>().Any();
    published.Should().BeTrue();
  }

  [Fact]
  public async Task DELETE_accounts_Returns404_WhenMissing()
  {
    var response = await _client.DeleteAsync($"/api/accounts/{Guid.NewGuid()}");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task GET_health_Returns200_Healthy()
  {
    var response = await _client.GetAsync("/health");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
  }
}
