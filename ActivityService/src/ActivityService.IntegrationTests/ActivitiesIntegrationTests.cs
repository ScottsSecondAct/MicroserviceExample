using FluentAssertions;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Activities.Events;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ActivityService.IntegrationTests;

public class ActivitiesIntegrationTests : IClassFixture<ActivityServiceFactory>
{
  private readonly HttpClient _client;
  private readonly ITestHarness _harness;

  public ActivitiesIntegrationTests(ActivityServiceFactory factory)
  {
    _client = factory.CreateClient();
    _harness = factory.Services.GetRequiredService<ITestHarness>();
  }

  [Fact]
  public async Task POST_activities_Returns201_And_PublishesActivityLogged()
  {
    var response = await _client.PostAsJsonAsync("/api/activities",
      new { type = "Call", subject = "Discovery call", notes = "Initial contact" });

    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var body = await response.Content.ReadAsStringAsync();
    var doc = JsonDocument.Parse(body);
    doc.RootElement.GetProperty("subject").GetString().Should().Be("Discovery call");
    doc.RootElement.GetProperty("type").GetString().Should().Be("Call");
    var published = await _harness.Published.SelectAsync<ActivityLogged>().Any();
    published.Should().BeTrue();
  }

  [Fact]
  public async Task POST_activities_WithMissingSubject_Returns400()
  {
    var response = await _client.PostAsJsonAsync("/api/activities", new { type = "Email", subject = "" });

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task GET_activities_Returns200_WithList()
  {
    await _client.PostAsJsonAsync("/api/activities", new { type = "Note", subject = "Listed note" });

    var response = await _client.GetAsync("/api/activities");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    arr.ValueKind.Should().Be(JsonValueKind.Array);
  }

  [Fact]
  public async Task GET_activities_ById_Returns200_WithAllFields()
  {
    var created = await _client.PostAsJsonAsync("/api/activities",
      new { type = "Meeting", subject = "Kickoff meeting", notes = "Important" });
    var activityId = JsonDocument.Parse(await created.Content.ReadAsStringAsync())
      .RootElement.GetProperty("activityId").GetGuid();

    var response = await _client.GetAsync($"/api/activities/{activityId}");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    doc.RootElement.GetProperty("activityId").GetGuid().Should().Be(activityId);
    doc.RootElement.GetProperty("subject").GetString().Should().Be("Kickoff meeting");
  }

  [Fact]
  public async Task GET_activities_ById_Returns404_WhenMissing()
  {
    var response = await _client.GetAsync($"/api/activities/{Guid.NewGuid()}");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task GET_activities_FilterByType_ReturnsOnlyMatching()
  {
    await _client.PostAsJsonAsync("/api/activities", new { type = "Task", subject = "Task activity" });
    await _client.PostAsJsonAsync("/api/activities", new { type = "Email", subject = "Email activity" });

    var response = await _client.GetAsync("/api/activities?type=Task");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    arr.ValueKind.Should().Be(JsonValueKind.Array);
    foreach (var item in arr.EnumerateArray())
      item.GetProperty("type").GetString().Should().Be("Task");
  }

  [Fact]
  public async Task PUT_activities_Updates_Returns200()
  {
    var created = await _client.PostAsJsonAsync("/api/activities",
      new { type = "Call", subject = "Original subject" });
    var activityId = JsonDocument.Parse(await created.Content.ReadAsStringAsync())
      .RootElement.GetProperty("activityId").GetGuid();

    var response = await _client.PutAsJsonAsync($"/api/activities/{activityId}",
      new { subject = "Updated subject" });

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    doc.RootElement.GetProperty("subject").GetString().Should().Be("Updated subject");
  }

  [Fact]
  public async Task PUT_activities_CompleteTask_Returns200_And_PublishesTaskCompleted()
  {
    var created = await _client.PostAsJsonAsync("/api/activities",
      new { type = "Task", subject = "A task to complete" });
    var activityId = JsonDocument.Parse(await created.Content.ReadAsStringAsync())
      .RootElement.GetProperty("activityId").GetGuid();

    var response = await _client.PutAsJsonAsync($"/api/activities/{activityId}",
      new { completedAt = DateTime.UtcNow });

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    doc.RootElement.GetProperty("completedAt").GetString().Should().NotBeNull();
    var published = await _harness.Published.SelectAsync<TaskCompleted>().Any();
    published.Should().BeTrue();
  }

  [Fact]
  public async Task DELETE_activities_Returns204()
  {
    var created = await _client.PostAsJsonAsync("/api/activities",
      new { type = "Note", subject = "Delete me" });
    var activityId = JsonDocument.Parse(await created.Content.ReadAsStringAsync())
      .RootElement.GetProperty("activityId").GetGuid();

    var response = await _client.DeleteAsync($"/api/activities/{activityId}");

    response.StatusCode.Should().Be(HttpStatusCode.NoContent);
  }

  [Fact]
  public async Task DELETE_activities_Returns404_WhenMissing()
  {
    var response = await _client.DeleteAsync($"/api/activities/{Guid.NewGuid()}");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task GET_health_Returns200_Healthy()
  {
    var response = await _client.GetAsync("/health");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
  }
}
