namespace ContactService.Services;

public class AccountClient : IAccountClient
{
  private readonly HttpClient _httpClient;
  private readonly ILogger<AccountClient> _logger;

  public AccountClient(HttpClient httpClient, ILogger<AccountClient> logger)
  {
    _httpClient = httpClient;
    _logger = logger;
  }

  public async Task<bool> AccountExistsAsync(Guid accountId)
  {
    try
    {
      var response = await _httpClient.GetAsync($"/api/accounts/{accountId}");
      return response.IsSuccessStatusCode;
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Failed to verify account {AccountId} with AccountService. Failing open.", accountId);
      return true;
    }
  }
}
