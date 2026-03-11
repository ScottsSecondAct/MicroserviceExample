using System.Net;
using System.Text.Json;
using EndToEnd.Tests.Infrastructure;
using FluentAssertions;

namespace EndToEnd.Tests.Flows;

public class AuthFlowTests : IDisposable
{
    private readonly GatewayClient _client = new();

    // Registers a user, logs in, sets the Bearer token, and returns the userId from /me.
    private async Task<string> RegisterAndLoginAsync(string email, string password)
    {
        await _client.PostAsync("/auth/api/registration/register", new { email, password });
        var loginResponse = await _client.PostAsync("/auth/api/login/login", new { email, password });
        var token = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("token").GetString()!;
        _client.SetToken(token);
        var me = JsonDocument.Parse(await (await _client.GetAsync("/auth/api/login/me")).Content.ReadAsStringAsync());
        return me.RootElement.GetProperty("userId").GetString()!;
    }

    [Fact]
    public async Task Register_CreatesUserProfile_ViaEventAsync()
    {
        var email = $"e2e-auth-{Guid.NewGuid()}@example.com";
        var password = "Password123!";

        var registerResponse = await _client.PostAsync("/auth/api/registration/register", new { email, password });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Login to get a token (role may be Unassigned until event is processed)
        var userId = await RegisterAndLoginAsync(email, password);

        // Poll until UserManagementService has processed the UserRegistered event
        await RetryHelper.WaitUntilAsync(async () =>
        {
            var r = await _client.GetAsync($"/users/api/users/{userId}");
            return r.StatusCode == HttpStatusCode.OK;
        }, timeout: TimeSpan.FromSeconds(15));

        var profileResponse = await _client.GetAsync($"/users/api/users/{userId}");
        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = JsonDocument.Parse(await profileResponse.Content.ReadAsStringAsync());
        // UserManagementService serializes Role as an integer (1 = Member per UserRole enum)
        profile.RootElement.GetProperty("role").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Login_ReturnsJwtWithCorrectEmailClaim()
    {
        var email = $"e2e-login-{Guid.NewGuid()}@example.com";
        var password = "Password123!";

        await _client.PostAsync("/auth/api/registration/register", new { email, password });

        var loginResponse = await _client.PostAsync("/auth/api/login/login", new { email, password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("token").GetString();
        token.Should().NotBeNullOrEmpty();

        _client.SetToken(token!);
        var meResponse = await _client.GetAsync("/auth/api/login/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = JsonDocument.Parse(await meResponse.Content.ReadAsStringAsync());
        me.RootElement.GetProperty("email").GetString().Should().Be(email);
        me.RootElement.GetProperty("userId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var email = $"e2e-dup-{Guid.NewGuid()}@example.com";
        await _client.PostAsync("/auth/api/registration/register", new { email, password = "Password123!" });

        var second = await _client.PostAsync("/auth/api/registration/register",
            new { email, password = "Password123!" });

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    public void Dispose() => _client.Dispose();
}
