using ContactService.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;

namespace ContactService.Tests.Services;

public class AccountClientTests
{
  private static HttpClient BuildClient(MockHttpMessageHandler handler)
  {
    var client = handler.ToHttpClient();
    client.BaseAddress = new Uri("http://test.local");
    return client;
  }

  private static ILogger<AccountClient> Logger() =>
    Mock.Of<ILogger<AccountClient>>();

  [Fact]
  public async Task AccountExistsAsync_ReturnsTrue_WhenAccountFound()
  {
    var accountId = Guid.NewGuid();

    var handler = new MockHttpMessageHandler();
    handler.When($"/api/accounts/{accountId}")
      .Respond(System.Net.HttpStatusCode.OK);

    var sut = new AccountClient(BuildClient(handler), Logger());

    var result = await sut.AccountExistsAsync(accountId);

    result.Should().BeTrue();
  }

  [Fact]
  public async Task AccountExistsAsync_ReturnsFalse_WhenAccountNotFound()
  {
    var accountId = Guid.NewGuid();

    var handler = new MockHttpMessageHandler();
    handler.When($"/api/accounts/{accountId}")
      .Respond(System.Net.HttpStatusCode.NotFound);

    var sut = new AccountClient(BuildClient(handler), Logger());

    var result = await sut.AccountExistsAsync(accountId);

    result.Should().BeFalse();
  }

  [Fact]
  public async Task AccountExistsAsync_ReturnsTrue_OnNetworkException_FailOpen()
  {
    var accountId = Guid.NewGuid();

    var handler = new MockHttpMessageHandler();
    handler.When($"/api/accounts/{accountId}")
      .Throw(new HttpRequestException("connection refused"));

    var sut = new AccountClient(BuildClient(handler), Logger());

    var result = await sut.AccountExistsAsync(accountId);

    result.Should().BeTrue("fail-open: network errors must not block contact creation");
  }
}
