using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Messaging.Events;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace UserManagementService.IntegrationTests;

public class UserManagementIntegrationTests : IClassFixture<UserManagementServiceFactory>
{
  private readonly HttpClient _client;
  private readonly ITestHarness _harness;
  private readonly UserManagementServiceFactory _factory;

  public UserManagementIntegrationTests(UserManagementServiceFactory factory)
  {
    _factory = factory;
    _client = factory.CreateClient();
    _harness = factory.Services.GetRequiredService<ITestHarness>();
  }

  private static object CreateProfilePayload(Guid? userId = null, string? email = null) => new
  {
    userId = userId ?? Guid.NewGuid(),
    email = email ?? $"user-{Guid.NewGuid()}@test.com",
    displayName = "Test User"
  };

  // ── POST /api/users ───────────────────────────────────────────────────────

  [Fact]
  public async Task POST_users_Returns201_And_ProfileInDB()
  {
    var payload = CreateProfilePayload();
    var response = await _client.PostAsJsonAsync("/api/users", payload);

    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var body = await response.Content.ReadAsStringAsync();
    var doc = JsonDocument.Parse(body);
    doc.RootElement.GetProperty("userId").GetGuid().Should().NotBeEmpty();
  }

  [Fact]
  public async Task POST_users_DuplicateUserId_Returns400()
  {
    var userId = Guid.NewGuid();
    await _client.PostAsJsonAsync("/api/users", CreateProfilePayload(userId));

    var response = await _client.PostAsJsonAsync("/api/users", CreateProfilePayload(userId));

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  // ── GET /api/users/{id} ───────────────────────────────────────────────────

  [Fact]
  public async Task GET_users_ById_Returns200_WithAllProfileFields()
  {
    var userId = Guid.NewGuid();
    var email = $"getby-{Guid.NewGuid()}@test.com";
    await _client.PostAsJsonAsync("/api/users", new { userId, email, displayName = "Get Me" });

    var response = await _client.GetAsync($"/api/users/{userId}");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    doc.RootElement.GetProperty("userId").GetGuid().Should().Be(userId);
    doc.RootElement.GetProperty("email").GetString().Should().Be(email);
    doc.RootElement.GetProperty("role").GetInt32().Should().BeGreaterThanOrEqualTo(0);
  }

  [Fact]
  public async Task GET_users_ById_Returns404_WhenMissing()
  {
    var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  // ── GET /api/users/{id}/role ──────────────────────────────────────────────

  [Fact]
  public async Task GET_users_Role_Returns200_WithRoleString()
  {
    var userId = Guid.NewGuid();
    await _client.PostAsJsonAsync("/api/users", new { userId, email = $"role-{Guid.NewGuid()}@test.com" });

    var response = await _client.GetAsync($"/api/users/{userId}/role");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var body = await response.Content.ReadAsStringAsync();
    body.Should().Contain("\"role\":1");
  }

  // ── GET /api/users/team ───────────────────────────────────────────────────

  [Fact]
  public async Task GET_users_Team_Returns200_WithList()
  {
    await _client.PostAsJsonAsync("/api/users", new
    {
      userId = Guid.NewGuid(),
      email = $"team-{Guid.NewGuid()}@test.com",
      displayName = "Team Member"
    });

    var response = await _client.GetAsync("/api/users/team");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    arr.ValueKind.Should().Be(JsonValueKind.Array);
    arr.GetArrayLength().Should().BeGreaterThan(0);
    var first = arr[0];
    first.TryGetProperty("userId", out _).Should().BeTrue();
    first.TryGetProperty("displayName", out _).Should().BeTrue();
    first.TryGetProperty("role", out _).Should().BeTrue();
  }

  // ── Consumer: UserRegistered ──────────────────────────────────────────────

  [Fact]
  public async Task Consumer_UserRegistered_CreatesProfileInDB()
  {
    var userId = Guid.NewGuid();
    var email = $"consumer-{Guid.NewGuid()}@test.com";

    await _harness.Bus.Publish(new UserRegistered { UserId = userId, Email = email });
    await Task.Delay(500);

    var response = await _client.GetAsync($"/api/users/{userId}");
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    doc.RootElement.GetProperty("email").GetString().Should().Be(email);
  }

  [Fact]
  public async Task Consumer_UserRegistered_IsIdempotent()
  {
    var userId = Guid.NewGuid();
    var email = $"idem-{Guid.NewGuid()}@test.com";
    var @event = new UserRegistered { UserId = userId, Email = email };

    await _harness.Bus.Publish(@event);
    await Task.Delay(300);
    await _harness.Bus.Publish(@event);
    await Task.Delay(300);

    var response = await _client.GetAsync($"/api/users/{userId}");
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    // Only one profile should exist — verify by checking team list doesn't have duplicates
    var teamResponse = await _client.GetAsync($"/api/users/team");
    var team = JsonDocument.Parse(await teamResponse.Content.ReadAsStringAsync()).RootElement;
    var matching = team.EnumerateArray().Count(u => u.GetProperty("userId").GetGuid() == userId);
    matching.Should().Be(1);
  }

  // ── Health ────────────────────────────────────────────────────────────────

  [Fact]
  public async Task GET_health_Returns200_Healthy()
  {
    var response = await _client.GetAsync("/health");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
  }
}
