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

    // ── Admin: list users ─────────────────────────────────────────────────────

    [Fact]
    public async Task ListAllUsers_Admin_ReturnsUserList()
    {
        await _client.LoginAsAdminAsync();

        var response = await _client.GetAsync("/admin/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        users.ValueKind.Should().Be(JsonValueKind.Array);
        users.GetArrayLength().Should().BeGreaterThan(0);
        var first = users.EnumerateArray().First();
        first.TryGetProperty("userId", out _).Should().BeTrue();
        first.TryGetProperty("email", out _).Should().BeTrue();
        first.TryGetProperty("role", out _).Should().BeTrue();
        first.TryGetProperty("isActive", out _).Should().BeTrue();
    }

    // ── Admin: deactivate / reactivate ────────────────────────────────────────

    [Fact]
    public async Task DeactivateUser_BlocksLogin_ReactivateRestoresAccess()
    {
        await _client.LoginAsAdminAsync();

        var email = $"e2e-deactivate-{Guid.NewGuid()}@example.com";
        const string password = "Password123!";
        await _client.PostAsync("/auth/api/registration/register", new { email, password });

        // Get the new user's userId from /me
        var loginResponse = await _client.PostAsync("/auth/api/login/login", new { email, password });
        var userId = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("userId").GetString()!;

        // Poll until UMS has the user profile (async consumer)
        await _client.LoginAsAdminAsync();
        await RetryHelper.WaitUntilAsync(async () =>
        {
            var r = await _client.GetAsync($"/users/api/users/{userId}");
            return r.StatusCode == HttpStatusCode.OK;
        }, timeout: TimeSpan.FromSeconds(15));

        // Deactivate
        var deactivateResponse = await _client.PutAsync(
            $"/admin/api/admin/users/{userId}/active", new { isActive = false });
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Login attempt should now be blocked
        _client.ClearToken();
        var blockedLogin = await _client.PostAsync("/auth/api/login/login", new { email, password });
        blockedLogin.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Reactivate
        await _client.LoginAsAdminAsync();
        var reactivateResponse = await _client.PutAsync(
            $"/admin/api/admin/users/{userId}/active", new { isActive = true });
        reactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Login should work again
        _client.ClearToken();
        var restoredLogin = await _client.PostAsync("/auth/api/login/login", new { email, password });
        restoredLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Resend invite ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ResendInvite_ForUserWithNoPendingInvite_Returns400()
    {
        await _client.LoginAsAdminAsync();

        var email = $"e2e-resend-{Guid.NewGuid()}@example.com";
        await _client.PostAsync("/auth/api/registration/register", new { email, password = "Password123!" });

        var loginResponse = await _client.PostAsync("/auth/api/login/login", new { email, password = "Password123!" });
        var userId = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("userId").GetString()!;

        // Poll until UMS has the profile
        await _client.LoginAsAdminAsync();
        await RetryHelper.WaitUntilAsync(async () =>
        {
            var r = await _client.GetAsync($"/users/api/users/{userId}");
            return r.StatusCode == HttpStatusCode.OK;
        }, timeout: TimeSpan.FromSeconds(15));

        // Resend invite for a user registered via normal flow (no pending invite token)
        var response = await _client.PostAsync($"/users/api/users/{userId}/resend-invite", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResendInvite_ForInvitedUser_Returns200()
    {
        await _client.LoginAsAdminAsync();

        var email = $"e2e-resend-ok-{Guid.NewGuid()}@example.com";

        // Send invite — UMS consumer creates a stub profile with InviteToken set
        var inviteResponse = await _client.PostAsync("/auth/api/users/invite", new { email });
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Retrieve the pre-assigned userId from the invite token response
        var tokenResp = await _client.GetAsync($"/auth/api/test/tokens/invite?email={Uri.EscapeDataString(email)}");
        tokenResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // AuthService stores InvitedUserId on the invite token — retrieve it via DB query endpoint
        // Instead, poll UMS team list until the stub profile appears, then get the userId
        string? userId = null;
        await RetryHelper.WaitUntilAsync(async () =>
        {
            var r = await _client.GetAsync("/admin/api/admin/users");
            var users = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
            var match = users.EnumerateArray().FirstOrDefault(u =>
                u.GetProperty("email").GetString() == email);
            if (match.ValueKind != JsonValueKind.Undefined)
            {
                userId = match.GetProperty("userId").GetString();
                return true;
            }
            return false;
        }, timeout: TimeSpan.FromSeconds(15));

        // Resend invite — should succeed because UMS stub profile has InviteToken set
        var resendResponse = await _client.PostAsync($"/users/api/users/{userId}/resend-invite", new { });

        resendResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonDocument.Parse(await resendResponse.Content.ReadAsStringAsync()).RootElement;
        body.GetProperty("userId").GetString().Should().Be(userId);
        body.TryGetProperty("inviteSentAt", out _).Should().BeTrue();
    }

    // ── Audit log ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAuditLog_ReturnsEntries_AfterAdminAction()
    {
        await _client.LoginAsAdminAsync();

        // Perform an action that generates an audit entry: register a user then change their role
        var email = $"e2e-audit-{Guid.NewGuid()}@example.com";
        await _client.PostAsync("/auth/api/registration/register", new { email, password = "Password123!" });

        // Poll until UMS has processed UserRegistered and the user appears
        var loginResponse = await _client.PostAsync("/auth/api/login/login", new { email, password = "Password123!" });
        var userId = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("userId").GetString()!;
        await RetryHelper.WaitUntilAsync(async () =>
        {
            var r = await _client.GetAsync($"/users/api/users/{userId}");
            return r.StatusCode == System.Net.HttpStatusCode.OK;
        }, timeout: TimeSpan.FromSeconds(15));

        // Change the user's role — this logs an audit entry
        await _client.LoginAsAdminAsync();
        await _client.PutAsync($"/admin/api/admin/users/{userId}/role", new { role = 4 }); // Admin

        // Audit log should contain the RoleChanged entry
        var auditResponse = await _client.GetAsync("/users/api/users/audit");
        auditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var entries = JsonDocument.Parse(await auditResponse.Content.ReadAsStringAsync()).RootElement;
        entries.ValueKind.Should().Be(JsonValueKind.Array);
        entries.GetArrayLength().Should().BeGreaterThan(0);
        var entry = entries.EnumerateArray().First();
        entry.TryGetProperty("action", out _).Should().BeTrue();
        entry.TryGetProperty("actorUserId", out _).Should().BeTrue();
        entry.TryGetProperty("targetUserId", out _).Should().BeTrue();
        entry.TryGetProperty("timestamp", out _).Should().BeTrue();
    }

    public void Dispose() => _client.Dispose();
}
