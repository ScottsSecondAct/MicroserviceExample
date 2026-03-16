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

        // Verify completedAt is set
        var getResponse = await _client.GetAsync($"/activities/api/activities/{activityId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var completed = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        completed.RootElement.GetProperty("completedAt").ValueKind.Should().NotBe(JsonValueKind.Null);
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

    public void Dispose() => _client.Dispose();
}
