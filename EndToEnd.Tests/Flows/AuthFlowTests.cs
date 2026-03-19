using System.Net;
using System.Text.Json;
using EndToEnd.Tests.Infrastructure;
using FluentAssertions;

namespace EndToEnd.Tests.Flows;

public class AuthFlowTests : IDisposable
{
    private readonly GatewayClient _client = new();

    // Logs in as the seeded admin and sets the Bearer token on the client.
    private async Task LoginAsAdminAsync()
    {
        var loginResponse = await _client.PostAsync("/auth/api/login/login",
            new { email = "admin@example.com", password = "Admin1234!" });
        var token = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("token").GetString()!;
        _client.SetToken(token);
    }

    // Registers a user (requires admin token already set), logs in as that user,
    // sets the Bearer token, and returns the userId from /me.
    private async Task<string> RegisterAndLoginAsync(string email, string password)
    {
        await LoginAsAdminAsync();
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

        await LoginAsAdminAsync();
        var registerResponse = await _client.PostAsync("/auth/api/registration/register", new { email, password });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await _client.PostAsync("/auth/api/login/login", new { email, password });
        var token = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("token").GetString()!;
        _client.SetToken(token);
        var me = JsonDocument.Parse(await (await _client.GetAsync("/auth/api/login/me")).Content.ReadAsStringAsync());
        var userId = me.RootElement.GetProperty("userId").GetString()!;

        // Poll until UserManagementService has processed the UserRegistered event
        await RetryHelper.WaitUntilAsync(async () =>
        {
            var r = await _client.GetAsync($"/users/api/users/{userId}");
            return r.StatusCode == HttpStatusCode.OK;
        }, timeout: TimeSpan.FromSeconds(15));

        var profileResponse = await _client.GetAsync($"/users/api/users/{userId}");
        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = JsonDocument.Parse(await profileResponse.Content.ReadAsStringAsync());
        profile.RootElement.GetProperty("role").GetString().Should().Be("Member");
    }

    [Fact]
    public async Task Login_ReturnsJwtWithCorrectEmailClaim()
    {
        var email = $"e2e-login-{Guid.NewGuid()}@example.com";
        var password = "Password123!";

        await LoginAsAdminAsync();
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
        await LoginAsAdminAsync();
        await _client.PostAsync("/auth/api/registration/register", new { email, password = "Password123!" });

        var second = await _client.PostAsync("/auth/api/registration/register",
            new { email, password = "Password123!" });

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── Refresh token ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_WithValidToken_Returns200_WithNewJwt()
    {
        var email = $"e2e-refresh-{Guid.NewGuid()}@example.com";
        await LoginAsAdminAsync();
        await _client.PostAsync("/auth/api/registration/register", new { email, password = "Password123!" });

        var loginResponse = await _client.PostAsync("/auth/api/login/login", new { email, password = "Password123!" });
        var loginDoc = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var refreshToken = loginDoc.RootElement.GetProperty("refreshToken").GetString();

        _client.ClearToken();
        var refreshResponse = await _client.PostAsync("/auth/api/login/refresh", new { refreshToken });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshDoc = JsonDocument.Parse(await refreshResponse.Content.ReadAsStringAsync());
        refreshDoc.RootElement.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
        refreshDoc.RootElement.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_Returns401()
    {
        var response = await _client.PostAsync("/auth/api/login/refresh",
            new { refreshToken = "not-a-real-token" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Change password ───────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_AllowsLoginWithNewPassword_BlocksOld()
    {
        var email = $"e2e-chpw-{Guid.NewGuid()}@example.com";
        const string oldPassword = "Password123!";
        const string newPassword = "NewPassword456!";

        await LoginAsAdminAsync();
        await _client.PostAsync("/auth/api/registration/register", new { email, password = oldPassword });

        var loginResponse = await _client.PostAsync("/auth/api/login/login", new { email, password = oldPassword });
        var token = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("token").GetString()!;
        _client.SetToken(token);

        var changeResponse = await _client.PostAsync("/auth/api/auth/change-password",
            new { newPassword });
        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // New password works
        _client.ClearToken();
        var newLoginResponse = await _client.PostAsync("/auth/api/login/login",
            new { email, password = newPassword });
        newLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Old password no longer works
        var oldLoginResponse = await _client.PostAsync("/auth/api/login/login",
            new { email, password = oldPassword });
        oldLoginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Forgot / reset password ───────────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_Returns200_ForRegisteredEmail()
    {
        var email = $"e2e-forgot-{Guid.NewGuid()}@example.com";
        await LoginAsAdminAsync();
        await _client.PostAsync("/auth/api/registration/register", new { email, password = "Password123!" });
        _client.ClearToken();

        var response = await _client.PostAsync("/auth/api/auth/forgot-password", new { email });

        // Always 200 — prevents email enumeration
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_Returns200_ForUnknownEmail()
    {
        var response = await _client.PostAsync("/auth/api/auth/forgot-password",
            new { email = $"nobody-{Guid.NewGuid()}@example.com" });

        // Always 200 — prevents email enumeration
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_Returns400_WithEmptyEmail()
    {
        var response = await _client.PostAsync("/auth/api/auth/forgot-password", new { email = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_Returns400()
    {
        var response = await _client.PostAsync("/auth/api/auth/reset-password",
            new { token = "invalid-token", newPassword = "NewPassword123!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Invite ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendInvite_Admin_Returns200()
    {
        await LoginAsAdminAsync();

        var response = await _client.PostAsync("/auth/api/users/invite",
            new { email = $"e2e-invite-{Guid.NewGuid()}@example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SendInvite_DuplicateEmail_Returns409()
    {
        var email = $"e2e-invite-dup-{Guid.NewGuid()}@example.com";
        await LoginAsAdminAsync();
        await _client.PostAsync("/auth/api/registration/register", new { email, password = "Password123!" });

        var response = await _client.PostAsync("/auth/api/users/invite", new { email });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AcceptInvite_WithInvalidToken_Returns400()
    {
        var response = await _client.PostAsync("/auth/api/registration/accept-invite",
            new { token = "not-a-real-token", password = "NewPassword123!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FullInviteRoundTrip_InviteAcceptLogin()
    {
        var email = $"e2e-invite-rt-{Guid.NewGuid()}@example.com";
        const string password = "InvitePass1!";
        await LoginAsAdminAsync();

        // Send invite
        var inviteResponse = await _client.PostAsync("/auth/api/users/invite", new { email });
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Retrieve token via test endpoint
        var tokenResponse = await _client.GetAsync($"/auth/api/test/tokens/invite?email={Uri.EscapeDataString(email)}");
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("token").GetString()!;

        // Accept invite
        _client.ClearToken();
        var acceptResponse = await _client.PostAsync("/auth/api/registration/accept-invite",
            new { token, password });
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Login with the new password
        var loginResponse = await _client.PostAsync("/auth/api/login/login", new { email, password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task FullPasswordResetRoundTrip_ForgotResetLogin()
    {
        var email = $"e2e-reset-rt-{Guid.NewGuid()}@example.com";
        const string oldPassword = "Password123!";
        const string newPassword = "ResetPass99!";
        await LoginAsAdminAsync();
        await _client.PostAsync("/auth/api/registration/register", new { email, password = oldPassword });
        _client.ClearToken();

        // Request reset
        var forgotResponse = await _client.PostAsync("/auth/api/auth/forgot-password", new { email });
        forgotResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Retrieve token via test endpoint
        await LoginAsAdminAsync();
        var tokenResponse = await _client.GetAsync($"/auth/api/test/tokens/password-reset?email={Uri.EscapeDataString(email)}");
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("token").GetString()!;

        // Reset password
        _client.ClearToken();
        var resetResponse = await _client.PostAsync("/auth/api/auth/reset-password",
            new { token, newPassword });
        resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Login with new password works
        var loginResponse = await _client.PostAsync("/auth/api/login/login", new { email, password = newPassword });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Old password no longer works
        var oldLoginResponse = await _client.PostAsync("/auth/api/login/login", new { email, password = oldPassword });
        oldLoginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public void Dispose() => _client.Dispose();
}
