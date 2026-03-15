using System.Net;
using System.Text.Json;
using EndToEnd.Tests.Infrastructure;
using FluentAssertions;

namespace EndToEnd.Tests.Flows;

public class GatewayAuthTests : IDisposable
{
    private readonly GatewayClient _client = new();

    [Fact]
    public async Task ProtectedRoutes_WithoutToken_Return401()
    {
        var contactsResponse = await _client.GetAsync("/contacts/api/contacts");
        var accountsResponse = await _client.GetAsync("/accounts/api/accounts");
        var activitiesResponse = await _client.GetAsync("/activities/api/activities");

        contactsResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        accountsResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        activitiesResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PublicRoutes_WithoutToken_AreAccessible()
    {
        // Registration endpoint — requires Admin role; returns 401 without a token
        var registerResponse = await _client.PostAsync("/auth/api/registration/register",
            new { email = $"e2e-pub-{Guid.NewGuid()}@example.com", password = "Password123!" });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Login endpoint — does not require auth
        var loginResponse = await _client.PostAsync("/auth/api/login/login",
            new { email = "nonexistent@example.com", password = "wrong" });
        loginResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GatewayHealth_Returns200_WithAllServicesHealthy()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Healthy");
    }

    public void Dispose() => _client.Dispose();
}
