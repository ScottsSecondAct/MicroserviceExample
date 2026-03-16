using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EndToEnd.Tests.Infrastructure;

public class GatewayClient : IDisposable
{
    private readonly HttpClient _http;

    private static readonly string AdminEmail =
        Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL") ?? "admin@example.com";
    private static readonly string AdminPassword =
        Environment.GetEnvironmentVariable("DEFAULT_ADMIN_PASSWORD") ?? "Admin1234!";

    public GatewayClient()
    {
        var baseUrl = Environment.GetEnvironmentVariable("E2E_GATEWAY_URL") ?? "http://localhost:5000";
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public void SetToken(string token) =>
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Logs in as the seeded admin and sets the Bearer token on this client.
    public async Task LoginAsAdminAsync()
    {
        var loginResponse = await PostAsync("/auth/api/login/login", new { email = AdminEmail, password = AdminPassword });
        var token = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("token").GetString()!;
        SetToken(token);
    }

    public void ClearToken() =>
        _http.DefaultRequestHeaders.Authorization = null;

    public Task<HttpResponseMessage> GetAsync(string path) => _http.GetAsync(path);

    public Task<HttpResponseMessage> PostAsync<T>(string path, T body) =>
        _http.PostAsJsonAsync(path, body);

    public Task<HttpResponseMessage> PutAsync<T>(string path, T body) =>
        _http.PutAsJsonAsync(path, body);

    public Task<HttpResponseMessage> DeleteAsync(string path) => _http.DeleteAsync(path);

    public void Dispose() => _http.Dispose();
}
