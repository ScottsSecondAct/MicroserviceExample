using DealService.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using System.Net;

namespace DealService.Tests.Services;

public class AccountClientTests
{
  private readonly MockHttpMessageHandler _mockHttp = new();
  private readonly Mock<ILogger<AccountClient>> _logger = new();

  private AccountClient CreateClient()
  {
    var httpClient = _mockHttp.ToHttpClient();
    httpClient.BaseAddress = new Uri("http://account-service");
    return new AccountClient(httpClient, _logger.Object);
  }

  [Fact]
  public async Task AccountExistsAsync_Returns_True_When_200()
  {
    var id = Guid.NewGuid();
    _mockHttp.When($"http://account-service/api/accounts/{id}").Respond(HttpStatusCode.OK);

    var result = await CreateClient().AccountExistsAsync(id);

    result.Should().BeTrue();
  }

  [Fact]
  public async Task AccountExistsAsync_Returns_False_When_404()
  {
    var id = Guid.NewGuid();
    _mockHttp.When($"http://account-service/api/accounts/{id}").Respond(HttpStatusCode.NotFound);

    var result = await CreateClient().AccountExistsAsync(id);

    result.Should().BeFalse();
  }

  [Fact]
  public async Task AccountExistsAsync_Returns_True_On_NetworkException()
  {
    var id = Guid.NewGuid();
    _mockHttp.When($"http://account-service/api/accounts/{id}").Throw(new HttpRequestException("network error"));

    var result = await CreateClient().AccountExistsAsync(id);

    result.Should().BeTrue();
  }
}
