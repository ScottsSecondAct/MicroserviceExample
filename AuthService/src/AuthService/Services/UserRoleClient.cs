using AuthService.Models.DTOs;
using SharedLibrary.Enums;
using System.Net.Http.Json;

namespace AuthService.Services;

public class UserRoleClient : IUserRoleClient
{
  private readonly HttpClient _httpClient;
  private readonly ILogger<UserRoleClient> _logger;

  public UserRoleClient(HttpClient httpClient, ILogger<UserRoleClient> logger)
  {
    _httpClient = httpClient;
    _logger = logger;
  }

  public async Task<UserRole> GetRoleAsync(Guid userId)
  {
    try
    {
      var response = await _httpClient.GetAsync($"/api/users/{userId}/role");
      if (!response.IsSuccessStatusCode)
      {
        _logger.LogWarning("Failed to fetch role for user {UserId}, defaulting to Unassigned", userId);
        return UserRole.Unassigned;
      }
      var result = await response.Content.ReadFromJsonAsync<UserRoleResponse>();
      return result?.Role ?? UserRole.Unassigned;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error fetching role for user {UserId}, defaulting to Unassigned", userId);
      return UserRole.Unassigned;
    }
  }
}
