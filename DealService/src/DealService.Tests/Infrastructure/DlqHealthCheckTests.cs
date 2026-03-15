using System.Net;
using DealService.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;

namespace DealService.Tests.Infrastructure;

public class DlqHealthCheckTests
{
    private static IConfiguration BuildConfig(string host = "localhost", int managementPort = 15672,
        string username = "guest", string password = "guest", string vhost = "/")
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMQ:Host"] = host,
                ["RabbitMQ:ManagementPort"] = managementPort.ToString(),
                ["RabbitMQ:Username"] = username,
                ["RabbitMQ:Password"] = password,
                ["RabbitMQ:VirtualHost"] = vhost
            })
            .Build();
    }

    private static IHttpClientFactory BuildFactory(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler(responseBody, statusCode);
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        return factory.Object;
    }

    private static HealthCheckContext BuildContext() =>
        new() { Registration = new HealthCheckRegistration("rabbitmq-dlq", Mock.Of<IHealthCheck>(), null, null) };

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenNoErrorQueues()
    {
        var json = """[{"name":"contact-deleted","messages":0}]""";
        var check = new DlqHealthCheck(BuildFactory(json), BuildConfig());

        var result = await check.CheckHealthAsync(BuildContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenErrorQueueHasMessages()
    {
        var json = """[{"name":"contact-deleted_error","messages":2}]""";
        var check = new DlqHealthCheck(BuildFactory(json), BuildConfig());

        var result = await check.CheckHealthAsync(BuildContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Data.Should().ContainKey("contact-deleted_error");
        result.Data["contact-deleted_error"].Should().Be(2L);
    }

    [Fact]
    public async Task CheckHealthAsync_IgnoresErrorQueues_WithZeroMessages()
    {
        var json = """[{"name":"contact-deleted_error","messages":0}]""";
        var check = new DlqHealthCheck(BuildFactory(json), BuildConfig());

        var result = await check.CheckHealthAsync(BuildContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenManagementApiReturnsNonSuccess()
    {
        var check = new DlqHealthCheck(BuildFactory(string.Empty, HttpStatusCode.Forbidden), BuildConfig());

        var result = await check.CheckHealthAsync(BuildContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("403");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenManagementApiUnreachable()
    {
        var handler = new ThrowingHttpMessageHandler(new HttpRequestException("Connection refused"));
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var check = new DlqHealthCheck(factory.Object, BuildConfig());

        var result = await check.CheckHealthAsync(BuildContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("unreachable");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenConfigKeysAbsent()
    {
        var emptyConfig = new ConfigurationBuilder().Build();
        var json = """[]""";
        var check = new DlqHealthCheck(BuildFactory(json), emptyConfig);

        var result = await check.CheckHealthAsync(BuildContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenApiReturnsNullJson()
    {
        var check = new DlqHealthCheck(BuildFactory("null"), BuildConfig());

        var result = await check.CheckHealthAsync(BuildContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenTaskCanceled()
    {
        var handler = new ThrowingHttpMessageHandler(new TaskCanceledException("timeout"));
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var check = new DlqHealthCheck(factory.Object, BuildConfig());

        var result = await check.CheckHealthAsync(BuildContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("unreachable");
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseContent;
        private readonly HttpStatusCode _statusCode;

        public MockHttpMessageHandler(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _responseContent = responseContent;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent)
            });
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Exception _exception;
        public ThrowingHttpMessageHandler(Exception exception) => _exception = exception;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw _exception;
    }
}
