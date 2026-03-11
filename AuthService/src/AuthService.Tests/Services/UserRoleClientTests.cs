using AuthService.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using SharedLibrary.Enums;
using System.Text.Json;

namespace AuthService.Tests.Services;

public class UserRoleClientTests
{
  private static HttpClient BuildClient(MockHttpMessageHandler handler)
  {
    var client = handler.ToHttpClient();
    client.BaseAddress = new Uri("http://test.local");
    return client;
  }

  private static ILogger<UserRoleClient> Logger() =>
    Mock.Of<ILogger<UserRoleClient>>();

  [Fact]
  public async Task GetRoleAsync_ReturnsRole_OnSuccessResponse()
  {
    var userId = Guid.NewGuid();
    var payload = JsonSerializer.Serialize(new { userId, role = (int)UserRole.Member });

    var handler = new MockHttpMessageHandler();
    handler.When($"/api/users/{userId}/role")
      .Respond("application/json", payload);

    var sut = new UserRoleClient(BuildClient(handler), Logger());

    var result = await sut.GetRoleAsync(userId);

    result.Should().Be(UserRole.Member);
  }

  [Fact]
  public async Task GetRoleAsync_ReturnsAdminRole_WhenResponseContainsAdmin()
  {
    var userId = Guid.NewGuid();
    var payload = JsonSerializer.Serialize(new { userId, role = (int)UserRole.Admin });

    var handler = new MockHttpMessageHandler();
    handler.When($"/api/users/{userId}/role")
      .Respond("application/json", payload);

    var sut = new UserRoleClient(BuildClient(handler), Logger());

    var result = await sut.GetRoleAsync(userId);

    result.Should().Be(UserRole.Admin);
  }

  [Fact]
  public async Task GetRoleAsync_ReturnsUnassigned_WhenResponseIsNotSuccess()
  {
    var userId = Guid.NewGuid();

    var handler = new MockHttpMessageHandler();
    handler.When($"/api/users/{userId}/role")
      .Respond(System.Net.HttpStatusCode.NotFound);

    var sut = new UserRoleClient(BuildClient(handler), Logger());

    var result = await sut.GetRoleAsync(userId);

    result.Should().Be(UserRole.Unassigned);
  }

  [Fact]
  public async Task GetRoleAsync_ReturnsUnassigned_OnNetworkException()
  {
    var userId = Guid.NewGuid();

    var handler = new MockHttpMessageHandler();
    handler.When($"/api/users/{userId}/role")
      .Throw(new HttpRequestException("connection refused"));

    var sut = new UserRoleClient(BuildClient(handler), Logger());

    var result = await sut.GetRoleAsync(userId);

    result.Should().Be(UserRole.Unassigned);
  }
}
