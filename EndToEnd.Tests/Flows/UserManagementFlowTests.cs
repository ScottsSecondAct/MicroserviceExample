using System.Net;
using System.Text.Json;
using EndToEnd.Tests.Infrastructure;
using FluentAssertions;

namespace EndToEnd.Tests.Flows;

public class UserManagementFlowTests : IDisposable
{
    private readonly GatewayClient _client = new();

    [Fact]
    public async Task GetTeam_ReturnsUserList()
    {
        await _client.LoginAsAdminAsync();

        var response = await _client.GetAsync("/users/api/users/team");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var team = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        team.ValueKind.Should().Be(JsonValueKind.Array);
        team.GetArrayLength().Should().BeGreaterThan(0);

        // Each member has userId, displayName, role
        var first = team.EnumerateArray().First();
        first.TryGetProperty("userId", out _).Should().BeTrue();
        first.TryGetProperty("displayName", out _).Should().BeTrue();
        first.TryGetProperty("role", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetUserRole_ReturnsRoleForExistingUser()
    {
        await _client.LoginAsAdminAsync();

        // Get admin userId from the JWT /me endpoint
        var me = JsonDocument.Parse(
            await (await _client.GetAsync("/auth/api/login/me")).Content.ReadAsStringAsync());
        var adminUserId = me.RootElement.GetProperty("userId").GetString()!;

        var response = await _client.GetAsync($"/users/api/users/{adminUserId}/role");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var roleResponse = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        roleResponse.GetProperty("userId").GetString().Should().Be(adminUserId);
        roleResponse.TryGetProperty("role", out _).Should().BeTrue();
        roleResponse.TryGetProperty("isActive", out _).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterUser_AppearsInTeamList()
    {
        await _client.LoginAsAdminAsync();

        var email = $"e2e-team-{Guid.NewGuid()}@example.com";
        await _client.PostAsync("/auth/api/registration/register", new { email, password = "Password123!" });

        // Login as new user to get their userId
        var loginResponse = await _client.PostAsync("/auth/api/login/login", new { email, password = "Password123!" });
        var token = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("token").GetString()!;
        _client.SetToken(token);
        var me = JsonDocument.Parse(
            await (await _client.GetAsync("/auth/api/login/me")).Content.ReadAsStringAsync());
        var newUserId = me.RootElement.GetProperty("userId").GetGuid();

        // Switch back to admin and poll until UserManagementService processes UserRegistered event
        await _client.LoginAsAdminAsync();
        await RetryHelper.WaitUntilAsync(async () =>
        {
            var r = await _client.GetAsync("/users/api/users/team");
            var team = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
            return team.EnumerateArray().Any(m => m.GetProperty("userId").GetGuid() == newUserId);
        }, timeout: TimeSpan.FromSeconds(15));

        var teamResponse = await _client.GetAsync("/users/api/users/team");
        var teamArr = JsonDocument.Parse(await teamResponse.Content.ReadAsStringAsync()).RootElement;
        teamArr.EnumerateArray().Should().Contain(m => m.GetProperty("userId").GetGuid() == newUserId);
    }

    public void Dispose() => _client.Dispose();
}
