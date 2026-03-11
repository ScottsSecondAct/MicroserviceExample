using DealService.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using System.Net;

namespace DealService.Tests.Services;

public class ContactClientTests
{
  private readonly MockHttpMessageHandler _mockHttp = new();
  private readonly Mock<ILogger<ContactClient>> _logger = new();

  private ContactClient CreateClient()
  {
    var httpClient = _mockHttp.ToHttpClient();
    httpClient.BaseAddress = new Uri("http://contact-service");
    return new ContactClient(httpClient, _logger.Object);
  }

  [Fact]
  public async Task ContactExistsAsync_Returns_True_When_200()
  {
    var id = Guid.NewGuid();
    _mockHttp.When($"http://contact-service/api/contacts/{id}").Respond(HttpStatusCode.OK);

    var result = await CreateClient().ContactExistsAsync(id);

    result.Should().BeTrue();
  }

  [Fact]
  public async Task ContactExistsAsync_Returns_False_When_404()
  {
    var id = Guid.NewGuid();
    _mockHttp.When($"http://contact-service/api/contacts/{id}").Respond(HttpStatusCode.NotFound);

    var result = await CreateClient().ContactExistsAsync(id);

    result.Should().BeFalse();
  }

  [Fact]
  public async Task ContactExistsAsync_Returns_True_On_NetworkException()
  {
    var id = Guid.NewGuid();
    _mockHttp.When($"http://contact-service/api/contacts/{id}").Throw(new HttpRequestException("network error"));

    var result = await CreateClient().ContactExistsAsync(id);

    result.Should().BeTrue();
  }
}
