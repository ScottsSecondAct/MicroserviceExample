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

  private async Task SeedProfile(Guid userId, string email)
  {
    await _harness.Bus.Publish(new UserRegistered { UserId = userId, Email = email });
    await Task.Delay(500);
  }

  // ── GET /api/users/{id} ───────────────────────────────────────────────────

  [Fact]
  public async Task GET_users_ById_Returns200_WithAllProfileFields()
  {
    var userId = Guid.NewGuid();
    var email = $"getby-{Guid.NewGuid()}@test.com";
    await SeedProfile(userId, email);

    var response = await _client.GetAsync($"/api/users/{userId}");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    doc.RootElement.GetProperty("userId").GetGuid().Should().Be(userId);
    doc.RootElement.GetProperty("email").GetString().Should().Be(email);
    doc.RootElement.GetProperty("role").GetString().Should().NotBeNullOrEmpty();
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
    await SeedProfile(userId, $"role-{Guid.NewGuid()}@test.com");

    var response = await _client.GetAsync($"/api/users/{userId}/role");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var body = await response.Content.ReadAsStringAsync();
    body.Should().Contain("\"role\":\"Member\""); // New profiles are created as Member
  }

  // ── GET /api/users/team ───────────────────────────────────────────────────

  [Fact]
  public async Task GET_users_Team_Returns200_WithList()
  {
    await SeedProfile(Guid.NewGuid(), $"team-{Guid.NewGuid()}@test.com");

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

  // ── Consumer: UserInvited ─────────────────────────────────────────────────

  [Fact]
  public async Task Consumer_UserInvited_CreatesStubProfileAndAuditEntry()
  {
    var invitedUserId = Guid.NewGuid();
    var invitedByUserId = Guid.NewGuid();
    var email = $"invited-{Guid.NewGuid()}@test.com";

    // Seed the actor so audit FK resolves
    await SeedProfile(invitedByUserId, $"actor-{Guid.NewGuid()}@test.com");

    await _harness.Bus.Publish(new UserInvited
    {
      InvitedUserId = invitedUserId,
      Email = email,
      InvitedByUserId = invitedByUserId
    });
    await Task.Delay(500);

    // Stub profile should exist as inactive / Unassigned
    var profileResponse = await _client.GetAsync($"/api/users/{invitedUserId}");
    profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var doc = JsonDocument.Parse(await profileResponse.Content.ReadAsStringAsync());
    doc.RootElement.GetProperty("email").GetString().Should().Be(email);
    doc.RootElement.GetProperty("isActive").GetBoolean().Should().BeFalse();

    // Audit log should contain an InviteSent entry
    var adminClient = CreateAdminClient();
    var auditResponse = await adminClient.GetAsync("/api/users/audit");
    auditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var entries = JsonDocument.Parse(await auditResponse.Content.ReadAsStringAsync()).RootElement;
    entries.EnumerateArray().Should().Contain(e =>
      e.GetProperty("action").GetString() == "InviteSent" &&
      e.GetProperty("targetUserId").GetGuid() == invitedUserId);
  }

  // ── GET /api/admin/users ──────────────────────────────────────────────────

  [Fact]
  public async Task GET_admin_users_Returns401_WithoutAuth()
  {
    var response = await _client.GetAsync("/api/admin/users");

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task GET_admin_users_Returns200_WithAdminJwt()
  {
    var adminClient = CreateAdminClient();

    var response = await adminClient.GetAsync("/api/admin/users");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    arr.ValueKind.Should().Be(JsonValueKind.Array);
  }

  [Fact]
  public async Task GET_admin_users_ReturnsAllFields()
  {
    var userId = Guid.NewGuid();
    var email = $"admin-list-{Guid.NewGuid()}@test.com";
    await SeedProfile(userId, email);

    var adminClient = CreateAdminClient();
    var response = await adminClient.GetAsync("/api/admin/users");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    var match = arr.EnumerateArray().FirstOrDefault(u => u.GetProperty("userId").GetGuid() == userId);
    match.ValueKind.Should().NotBe(JsonValueKind.Undefined);
    match.TryGetProperty("email", out _).Should().BeTrue();
    match.TryGetProperty("displayName", out _).Should().BeTrue();
    match.TryGetProperty("role", out _).Should().BeTrue();
    match.TryGetProperty("isActive", out _).Should().BeTrue();
  }

  // ── PUT /api/admin/users/{id}/role ────────────────────────────────────────

  [Fact]
  public async Task PUT_admin_users_role_Returns401_WithoutAuth()
  {
    var userId = Guid.NewGuid();
    var response = await _client.PutAsJsonAsync($"/api/admin/users/{userId}/role", new { role = 1 });

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task PUT_admin_users_role_Returns200_WhenRoleUpdated()
  {
    var userId = Guid.NewGuid();
    await SeedProfile(userId, $"role-update-{Guid.NewGuid()}@test.com");

    var adminClient = CreateAdminClient();
    var response = await adminClient.PutAsJsonAsync($"/api/admin/users/{userId}/role", new { role = 4 }); // Admin

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    doc.RootElement.GetProperty("role").GetString().Should().Be("Admin");
  }

  [Fact]
  public async Task PUT_admin_users_role_Returns400_WhenRoleIsUnassigned()
  {
    var userId = Guid.NewGuid();
    await SeedProfile(userId, $"role-unassigned-{Guid.NewGuid()}@test.com");

    var adminClient = CreateAdminClient();
    var response = await adminClient.PutAsJsonAsync($"/api/admin/users/{userId}/role", new { role = 0 }); // Unassigned

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  // ── PUT /api/admin/users/{id}/active ─────────────────────────────────────

  [Fact]
  public async Task PUT_admin_users_active_Returns401_WithoutAuth()
  {
    var userId = Guid.NewGuid();
    var response = await _client.PutAsJsonAsync($"/api/admin/users/{userId}/active", new { isActive = false });

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task PUT_admin_users_active_Returns200_WhenStatusUpdated()
  {
    var userId = Guid.NewGuid();
    await SeedProfile(userId, $"deactivate-{Guid.NewGuid()}@test.com");

    var adminClient = CreateAdminClient();
    var response = await adminClient.PutAsJsonAsync($"/api/admin/users/{userId}/active", new { isActive = false });

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    doc.RootElement.GetProperty("isActive").GetBoolean().Should().BeFalse();
  }

  // ── Health ────────────────────────────────────────────────────────────────

  [Fact]
  public async Task GET_health_Returns200_Healthy()
  {
    var response = await _client.GetAsync("/health");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  private HttpClient CreateAdminClient()
  {
    var token = _factory.CreateAdminJwt();
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization =
      new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    return client;
  }
}
