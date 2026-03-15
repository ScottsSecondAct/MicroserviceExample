using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using UserManagementService.Infrastructure;

namespace UserManagementService.Tests.Infrastructure;

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
        var json = """[{"name":"user-management-service","messages":0}]""";
        var check = new DlqHealthCheck(BuildFactory(json), BuildConfig());

        var result = await check.CheckHealthAsync(BuildContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenErrorQueueHasMessages()
    {
        var json = """[{"name":"user-registered_error","messages":3},{"name":"user-registered","messages":0}]""";
        var check = new DlqHealthCheck(BuildFactory(json), BuildConfig());

        var result = await check.CheckHealthAsync(BuildContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Data.Should().ContainKey("user-registered_error");
        result.Data["user-registered_error"].Should().Be(3L);
    }

    [Fact]
    public async Task CheckHealthAsync_IgnoresErrorQueues_WithZeroMessages()
    {
        var json = """[{"name":"user-registered_error","messages":0}]""";
        var check = new DlqHealthCheck(BuildFactory(json), BuildConfig());

        var result = await check.CheckHealthAsync(BuildContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenManagementApiReturnsNonSuccess()
    {
        var check = new DlqHealthCheck(BuildFactory(string.Empty, HttpStatusCode.Unauthorized), BuildConfig());

        var result = await check.CheckHealthAsync(BuildContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("401");
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
    public async Task CheckHealthAsync_ReportsDegraded_WithMultipleErrorQueues()
    {
        var json = """
            [
              {"name":"queue-a_error","messages":1},
              {"name":"queue-b_error","messages":5},
              {"name":"queue-c","messages":10}
            ]
            """;
        var check = new DlqHealthCheck(BuildFactory(json), BuildConfig());

        var result = await check.CheckHealthAsync(BuildContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Data.Should().HaveCount(2);
        result.Data.Should().ContainKey("queue-a_error");
        result.Data.Should().ContainKey("queue-b_error");
        result.Data.Should().NotContainKey("queue-c");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenConfigKeysAbsent()
    {
        var emptyConfig = new ConfigurationBuilder().Build();
        var check = new DlqHealthCheck(BuildFactory("""[]"""), emptyConfig);

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
