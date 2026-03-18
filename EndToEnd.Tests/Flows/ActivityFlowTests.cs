using System.Net;
using System.Text.Json;
using EndToEnd.Tests.Infrastructure;
using FluentAssertions;

namespace EndToEnd.Tests.Flows;

public class ActivityFlowTests : IDisposable
{
    private readonly GatewayClient _client = new();

    private async Task<Guid> LoginAndCreateContactAsync()
    {
        await _client.LoginAsAdminAsync();

        var contactResponse = await _client.PostAsync("/contacts/api/contacts",
            new { firstName = "Activity", lastName = "Owner", email = $"act-owner-{Guid.NewGuid()}@example.com" });
        return JsonDocument.Parse(await contactResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("contactId").GetGuid();
    }

    [Fact]
    public async Task LogAndCompleteTask()
    {
        var contactId = await LoginAndCreateContactAsync();

        // Log a Task activity
        var createResponse = await _client.PostAsync("/activities/api/activities",
            new { type = "Task", subject = "E2E Follow-up task", contactId });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var activityId = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("activityId").GetGuid();

        // Filter by contactId and type=Task — task appears with completedAt null
        var listResponse = await _client.GetAsync($"/activities/api/activities?contactId={contactId}&type=Task");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasks = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync()).RootElement;
        tasks.ValueKind.Should().Be(JsonValueKind.Array);
        var task = tasks.EnumerateArray().First(t => t.GetProperty("activityId").GetGuid() == activityId);
        task.GetProperty("completedAt").ValueKind.Should().Be(JsonValueKind.Null);

        // Complete the task
        var completeResponse = await _client.PutAsync($"/activities/api/activities/{activityId}",
            new { completedAt = DateTime.UtcNow });
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify completedAt is set via list filter
        var verifyResponse = await _client.GetAsync($"/activities/api/activities?contactId={contactId}&type=Task");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var verifyArr = JsonDocument.Parse(await verifyResponse.Content.ReadAsStringAsync()).RootElement;
        var completed = verifyArr.EnumerateArray().First(a => a.GetProperty("activityId").GetGuid() == activityId);
        completed.GetProperty("completedAt").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task ActivityTimeline_FilterByDealId()
    {
        await _client.LoginAsAdminAsync();

        // Create a deal to associate activities with
        var dealResponse = await _client.PostAsync("/deals/api/deals",
            new { title = "Timeline Deal", value = 1000 });
        var dealId = JsonDocument.Parse(await dealResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("dealId").GetGuid();

        // Log Call, Email, and Note against the deal
        await _client.PostAsync("/activities/api/activities",
            new { type = "Call", subject = "Discovery call", dealId });
        await _client.PostAsync("/activities/api/activities",
            new { type = "Email", subject = "Follow-up email", dealId });
        await _client.PostAsync("/activities/api/activities",
            new { type = "Note", subject = "Meeting notes", dealId });

        // Filter by dealId — all three activities returned
        var timelineResponse = await _client.GetAsync($"/activities/api/activities?dealId={dealId}");
        timelineResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var activities = JsonDocument.Parse(await timelineResponse.Content.ReadAsStringAsync()).RootElement;
        activities.ValueKind.Should().Be(JsonValueKind.Array);
        activities.GetArrayLength().Should().Be(3);

        // Verify ordered by createdAt descending (most recent first)
        var dates = activities.EnumerateArray()
            .Select(a => a.GetProperty("createdAt").GetDateTime())
            .ToList();
        dates.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task DeleteActivity_Returns204_AndActivityIsGone()
    {
        var contactId = await LoginAndCreateContactAsync();

        var createResponse = await _client.PostAsync("/activities/api/activities",
            new { type = "Note", subject = "Activity to delete", contactId });
        var activityId = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("activityId").GetGuid();

        var deleteResponse = await _client.DeleteAsync($"/activities/api/activities/{activityId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify gone via list filter
        var listResponse = await _client.GetAsync($"/activities/api/activities?contactId={contactId}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var remaining = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync()).RootElement;
        remaining.EnumerateArray().Should().NotContain(a => a.GetProperty("activityId").GetGuid() == activityId);
    }

    [Fact]
    public async Task TaskCompleted_FiresOnlyOnFirstCompletion()
    {
        var contactId = await LoginAndCreateContactAsync();

        // Create a Task activity
        var createResponse = await _client.PostAsync("/activities/api/activities",
            new { type = "Task", subject = "Idempotent task", contactId });
        var activityId = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("activityId").GetGuid();

        // First completion — fires TaskCompleted event
        var firstComplete = await _client.PutAsync($"/activities/api/activities/{activityId}",
            new { completedAt = DateTime.UtcNow });
        firstComplete.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterFirstList = JsonDocument.Parse(await (await _client.GetAsync($"/activities/api/activities?contactId={contactId}&type=Task")).Content.ReadAsStringAsync()).RootElement;
        afterFirstList.EnumerateArray().First(a => a.GetProperty("activityId").GetGuid() == activityId)
            .GetProperty("completedAt").ValueKind.Should().NotBe(JsonValueKind.Null);

        // Second completion — does NOT fire TaskCompleted again (idempotent)
        var secondComplete = await _client.PutAsync($"/activities/api/activities/{activityId}",
            new { completedAt = DateTime.UtcNow });
        secondComplete.StatusCode.Should().Be(HttpStatusCode.OK);

        // Activity should still show completedAt set, no error
        var afterSecondList = JsonDocument.Parse(await (await _client.GetAsync($"/activities/api/activities?contactId={contactId}&type=Task")).Content.ReadAsStringAsync()).RootElement;
        afterSecondList.EnumerateArray().First(a => a.GetProperty("activityId").GetGuid() == activityId)
            .GetProperty("completedAt").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task ActivityLogged_AppearsInReportingActivitiesProjection()
    {
        await _client.LoginAsAdminAsync();

        // Get the admin's userId so we can set it as ownerId on the activity
        var me = JsonDocument.Parse(
            await (await _client.GetAsync("/auth/api/login/me")).Content.ReadAsStringAsync());
        var adminUserId = me.RootElement.GetProperty("userId").GetGuid();

        // Create an activity owned by the admin — fires ActivityLogged with OwnerId = adminUserId
        var createResponse = await _client.PostAsync("/activities/api/activities",
            new { type = "Call", subject = "Reporting test call", ownerId = adminUserId });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Poll until ReportingService processes ActivityLogged and creates/updates the projection
        await RetryHelper.WaitUntilAsync(async () =>
        {
            var r = await _client.GetAsync("/reports/api/reports/activities");
            var arr = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
            return arr.EnumerateArray()
                .Any(p => p.GetProperty("ownerId").GetGuid() == adminUserId &&
                          p.GetProperty("totalCount").GetInt32() >= 1);
        }, timeout: TimeSpan.FromSeconds(15));

        var activitiesResponse = await _client.GetAsync("/reports/api/reports/activities");
        var projections = JsonDocument.Parse(await activitiesResponse.Content.ReadAsStringAsync()).RootElement;
        var adminProjection = projections.EnumerateArray()
            .First(p => p.GetProperty("ownerId").GetGuid() == adminUserId);
        adminProjection.GetProperty("totalCount").GetInt32().Should().BeGreaterThan(0);
    }

    public void Dispose() => _client.Dispose();
}
