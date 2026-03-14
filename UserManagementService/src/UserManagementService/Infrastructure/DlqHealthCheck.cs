using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace UserManagementService.Infrastructure;

public class DlqHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public DlqHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var host = _configuration["RabbitMQ:Host"] ?? "localhost";
        var managementPort = _configuration.GetValue<int>("RabbitMQ:ManagementPort", 15672);
        var username = _configuration["RabbitMQ:Username"] ?? "guest";
        var password = _configuration["RabbitMQ:Password"] ?? "guest";
        var vhost = Uri.EscapeDataString(_configuration["RabbitMQ:VirtualHost"] ?? "/");

        try
        {
            using var client = _httpClientFactory.CreateClient();
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            client.Timeout = TimeSpan.FromSeconds(5);

            var url = $"http://{host}:{managementPort}/api/queues/{vhost}";
            var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return HealthCheckResult.Degraded(
                    $"RabbitMQ management API returned {(int)response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var queues = JsonSerializer.Deserialize<List<QueueStats>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var errorQueues = queues?
                .Where(q => q.Name.EndsWith("_error", StringComparison.Ordinal) && q.Messages > 0)
                .ToList();

            if (errorQueues == null || errorQueues.Count == 0)
                return HealthCheckResult.Healthy("No messages in dead-letter queues.");

            var data = errorQueues.ToDictionary(q => q.Name, q => (object)q.Messages);
            return HealthCheckResult.Degraded(
                $"{errorQueues.Count} dead-letter queue(s) have unprocessed messages.",
                data: data);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return HealthCheckResult.Degraded($"RabbitMQ management API unreachable: {ex.Message}");
        }
    }

    private sealed class QueueStats
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public long Messages { get; set; }
    }
}
