using AuthService.Models.DTOs;
using SharedLibrary.Enums;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AuthService.Services;

public class UserRoleClient : IUserRoleClient
{
  private static readonly JsonSerializerOptions _jsonOptions = new()
  {
    Converters = { new JsonStringEnumConverter() }
  };

  private readonly HttpClient _httpClient;
  private readonly ILogger<UserRoleClient> _logger;

  public UserRoleClient(HttpClient httpClient, ILogger<UserRoleClient> logger)
  {
    _httpClient = httpClient;
    _logger = logger;
  }

  public async Task<UserRoleResponse> GetRoleAsync(Guid userId)
  {
    var fallback = new UserRoleResponse { UserId = userId, Role = UserRole.Unassigned, IsActive = true };
    try
    {
      var response = await _httpClient.GetAsync($"/api/users/{userId}/role");
      if (!response.IsSuccessStatusCode)
      {
        _logger.LogWarning("Failed to fetch role for user {UserId}, defaulting to Unassigned", userId);
        return fallback;
      }
      var result = await response.Content.ReadFromJsonAsync<UserRoleResponse>(_jsonOptions);
      return result ?? fallback;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error fetching role for user {UserId}, defaulting to Unassigned", userId);
      return fallback;
    }
  }
}
