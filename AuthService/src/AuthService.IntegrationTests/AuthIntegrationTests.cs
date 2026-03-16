using FluentAssertions;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Messaging.Events;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace AuthService.IntegrationTests;

public class AuthIntegrationTests : IClassFixture<AuthServiceFactory>
{
  private readonly HttpClient _client;
  private readonly ITestHarness _harness;
  private readonly AuthServiceFactory _factory;

  public AuthIntegrationTests(AuthServiceFactory factory)
  {
    _factory = factory;
    _client = factory.CreateClient();
    _harness = factory.Services.GetRequiredService<ITestHarness>();
  }

  private static readonly Guid AdminId = new("00000000-0000-0000-0000-000000000001");

  private void StubRoleEndpoint(Guid userId, int roleValue = 1, bool isActive = true) // 1 = Member
  {
    _factory.UmsMock
      .Given(Request.Create().WithPath($"/api/users/{userId}/role").UsingGet())
      .RespondWith(Response.Create()
        .WithStatusCode(200)
        .WithBody($"{{\"userId\":\"{userId}\",\"role\":{roleValue},\"isActive\":{isActive.ToString().ToLower()}}}")
        .WithHeader("Content-Type", "application/json"));
  }

  // Returns an Admin JWT by logging in as the seeded admin user.
  private async Task<string> GetAdminTokenAsync()
  {
    StubRoleEndpoint(AdminId, roleValue: 4); // 4 = Admin
    var loginResp = await _client.PostAsJsonAsync("/api/login/login",
      new { email = "admin@example.com", password = "Admin1234!" });
    loginResp.IsSuccessStatusCode.Should().BeTrue("admin login should succeed");
    return JsonDocument.Parse(await loginResp.Content.ReadAsStringAsync())
      .RootElement.GetProperty("token").GetString()!;
  }

  // Calls POST /api/registration/register authenticated as the seeded admin.
  private async Task<HttpResponseMessage> RegisterAsAdminAsync(object body)
  {
    var token = await GetAdminTokenAsync();
    var req = new HttpRequestMessage(HttpMethod.Post, "/api/registration/register")
    {
      Content = JsonContent.Create(body)
    };
    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    return await _client.SendAsync(req);
  }

  private async Task<Guid> RegisterAndGetUserId(string email, string password = "Test@1234")
  {
    var response = await RegisterAsAdminAsync(new { email, password });
    response.IsSuccessStatusCode.Should().BeTrue($"registration of {email} should succeed");

    // Retrieve the userId from the DB via the harness's published UserRegistered event
    var envelope = _harness.Published
      .SelectAsync<UserRegistered>(m => m.Context.Message.Email == email)
      .ToBlockingEnumerable()
      .FirstOrDefault();
    envelope.Should().NotBeNull("UserRegistered should have been published");
    return envelope!.Context.Message.UserId;
  }

  // ── Registration ──────────────────────────────────────────────────────────

  [Fact]
  public async Task POST_register_Returns200_And_PublishesUserRegistered()
  {
    var email = $"reg-{Guid.NewGuid()}@test.com";

    var response = await RegisterAsAdminAsync(new { email, password = "Test@1234" });

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var published = await _harness.Published.SelectAsync<UserRegistered>().Any();
    published.Should().BeTrue();
  }

  [Fact]
  public async Task POST_register_Duplicate_Returns409_NoSecondEvent()
  {
    var email = $"dup-{Guid.NewGuid()}@test.com";
    await RegisterAsAdminAsync(new { email, password = "Test@1234" });

    var countBefore = _harness.Published.SelectAsync<UserRegistered>().ToBlockingEnumerable().Count();
    var response = await RegisterAsAdminAsync(new { email, password = "Test@1234" });

    response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    var countAfter = _harness.Published.SelectAsync<UserRegistered>().ToBlockingEnumerable().Count();
    countAfter.Should().Be(countBefore);
  }

  // ── Login ─────────────────────────────────────────────────────────────────

  [Fact]
  public async Task POST_login_ValidCredentials_Returns200_WithJwt()
  {
    var email = $"login-{Guid.NewGuid()}@test.com";
    var userId = await RegisterAndGetUserId(email);
    StubRoleEndpoint(userId);

    var response = await _client.PostAsJsonAsync("/api/login/login",
      new { email, password = "Test@1234" });

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var body = await response.Content.ReadAsStringAsync();
    var doc = JsonDocument.Parse(body);
    doc.RootElement.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
  }

  [Fact]
  public async Task POST_login_WrongPassword_Returns401()
  {
    var email = $"wrongpw-{Guid.NewGuid()}@test.com";
    var userId = await RegisterAndGetUserId(email);
    StubRoleEndpoint(userId);

    var response = await _client.PostAsJsonAsync("/api/login/login",
      new { email, password = "WrongPassword!" });

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task POST_login_UnknownEmail_Returns401()
  {
    var response = await _client.PostAsJsonAsync("/api/login/login",
      new { email = "nobody@nowhere.com", password = "Test@1234" });

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  // ── /me ───────────────────────────────────────────────────────────────────

  [Fact]
  public async Task GET_me_WithValidToken_Returns200_WithClaims()
  {
    var email = $"me-{Guid.NewGuid()}@test.com";
    var userId = await RegisterAndGetUserId(email);
    StubRoleEndpoint(userId);

    var loginResponse = await _client.PostAsJsonAsync("/api/login/login",
      new { email, password = "Test@1234" });
    var token = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync())
      .RootElement.GetProperty("token").GetString();

    var request = new HttpRequestMessage(HttpMethod.Get, "/api/login/me");
    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    var response = await _client.SendAsync(request);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    doc.RootElement.GetProperty("email").GetString().Should().Be(email);
    doc.RootElement.GetProperty("role").GetString().Should().Be("Member");
  }

  [Fact]
  public async Task GET_me_WithNoToken_Returns401()
  {
    var response = await _client.GetAsync("/api/login/me");

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  // ── Refresh token ─────────────────────────────────────────────────────────

  [Fact]
  public async Task POST_refresh_ValidToken_Returns200_WithNewJwt()
  {
    var email = $"refresh-{Guid.NewGuid()}@test.com";
    var userId = await RegisterAndGetUserId(email);
    StubRoleEndpoint(userId);

    var loginResp = await _client.PostAsJsonAsync("/api/login/login", new { email, password = "Test@1234" });
    var loginDoc = JsonDocument.Parse(await loginResp.Content.ReadAsStringAsync());
    var refreshToken = loginDoc.RootElement.GetProperty("refreshToken").GetString();

    var response = await _client.PostAsJsonAsync("/api/login/refresh", new { refreshToken });

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    doc.RootElement.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
  }

  [Fact]
  public async Task POST_refresh_InvalidToken_Returns401()
  {
    var response = await _client.PostAsJsonAsync("/api/login/refresh",
      new { refreshToken = "totally-invalid-token" });

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task POST_refresh_EmptyToken_Returns4xx()
  {
    var response = await _client.PostAsJsonAsync("/api/login/refresh",
      new { refreshToken = "" });

    ((int)response.StatusCode).Should().BeInRange(400, 499);
  }

  // ── Health ────────────────────────────────────────────────────────────────

  [Fact]
  public async Task GET_health_Returns200_Healthy()
  {
    var response = await _client.GetAsync("/health");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
  }
}
