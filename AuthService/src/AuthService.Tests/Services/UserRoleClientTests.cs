using AuthService.Models.DTOs;
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
    var payload = JsonSerializer.Serialize(new { userId, role = (int)UserRole.Member, isActive = true });

    var handler = new MockHttpMessageHandler();
    handler.When($"/api/users/{userId}/role")
      .Respond("application/json", payload);

    var sut = new UserRoleClient(BuildClient(handler), Logger());

    var result = await sut.GetRoleAsync(userId);

    result.Role.Should().Be(UserRole.Member);
    result.IsActive.Should().BeTrue();
  }

  [Fact]
  public async Task GetRoleAsync_ReturnsAdminRole_WhenResponseContainsAdmin()
  {
    var userId = Guid.NewGuid();
    var payload = JsonSerializer.Serialize(new { userId, role = (int)UserRole.Admin, isActive = true });

    var handler = new MockHttpMessageHandler();
    handler.When($"/api/users/{userId}/role")
      .Respond("application/json", payload);

    var sut = new UserRoleClient(BuildClient(handler), Logger());

    var result = await sut.GetRoleAsync(userId);

    result.Role.Should().Be(UserRole.Admin);
    result.IsActive.Should().BeTrue();
  }

  [Fact]
  public async Task GetRoleAsync_ReturnsIsActiveFalse_WhenUserIsDeactivated()
  {
    var userId = Guid.NewGuid();
    var payload = JsonSerializer.Serialize(new { userId, role = (int)UserRole.Member, isActive = false });

    var handler = new MockHttpMessageHandler();
    handler.When($"/api/users/{userId}/role")
      .Respond("application/json", payload);

    var sut = new UserRoleClient(BuildClient(handler), Logger());

    var result = await sut.GetRoleAsync(userId);

    result.Role.Should().Be(UserRole.Member);
    result.IsActive.Should().BeFalse();
  }

  [Fact]
  public async Task GetRoleAsync_ReturnsUnassignedAndActive_WhenResponseIsNotSuccess()
  {
    var userId = Guid.NewGuid();

    var handler = new MockHttpMessageHandler();
    handler.When($"/api/users/{userId}/role")
      .Respond(System.Net.HttpStatusCode.NotFound);

    var sut = new UserRoleClient(BuildClient(handler), Logger());

    var result = await sut.GetRoleAsync(userId);

    result.Role.Should().Be(UserRole.Unassigned);
    result.IsActive.Should().BeTrue();
  }

  [Fact]
  public async Task GetRoleAsync_ReturnsUnassignedAndActive_OnNetworkException()
  {
    var userId = Guid.NewGuid();

    var handler = new MockHttpMessageHandler();
    handler.When($"/api/users/{userId}/role")
      .Throw(new HttpRequestException("connection refused"));

    var sut = new UserRoleClient(BuildClient(handler), Logger());

    var result = await sut.GetRoleAsync(userId);

    result.Role.Should().Be(UserRole.Unassigned);
    result.IsActive.Should().BeTrue();
  }
}
