using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ReportingService.Infrastructure;

namespace ReportingService.Tests.Infrastructure;

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
        return new TestHttpClientFactory(new HttpClient(handler));
    }

    private static HealthCheckContext BuildContext() =>
        new() { Registration = new HealthCheckRegistration("rabbitmq-dlq", new NoOpHealthCheck(), null, null) };

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenNoErrorQueues()
    {
        var json = """[{"name":"deal-created","messages":0},{"name":"activity-logged","messages":0}]""";
        var check = new DlqHealthCheck(BuildFactory(json), BuildConfig());

        var result = await check.CheckHealthAsync(BuildContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenErrorQueueHasMessages()
    {
        var json = """[{"name":"deal-created_error","messages":7}]""";
        var check = new DlqHealthCheck(BuildFactory(json), BuildConfig());

        var result = await check.CheckHealthAsync(BuildContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Data.Should().ContainKey("deal-created_error");
        result.Data["deal-created_error"].Should().Be(7L);
    }

    [Fact]
    public async Task CheckHealthAsync_IgnoresErrorQueues_WithZeroMessages()
    {
        var json = """[{"name":"deal-created_error","messages":0},{"name":"activity-logged_error","messages":0}]""";
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
        var check = new DlqHealthCheck(new TestHttpClientFactory(new HttpClient(handler)), BuildConfig());

        var result = await check.CheckHealthAsync(BuildContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("unreachable");
    }

    [Fact]
    public async Task CheckHealthAsync_ReportsDegraded_WithMultipleReportingErrorQueues()
    {
        var json = """
            [
              {"name":"deal-created_error","messages":2},
              {"name":"deal-stage-changed_error","messages":1},
              {"name":"deal-created","messages":0}
            ]
            """;
        var check = new DlqHealthCheck(BuildFactory(json), BuildConfig());

        var result = await check.CheckHealthAsync(BuildContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Data.Should().HaveCount(2);
        result.Data.Should().ContainKey("deal-created_error");
        result.Data.Should().ContainKey("deal-stage-changed_error");
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
        var check = new DlqHealthCheck(new TestHttpClientFactory(new HttpClient(handler)), BuildConfig());

        var result = await check.CheckHealthAsync(BuildContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("unreachable");
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public TestHttpClientFactory(HttpClient client) => _client = client;
        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class NoOpHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(HealthCheckResult.Healthy());
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
