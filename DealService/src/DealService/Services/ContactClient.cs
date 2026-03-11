namespace DealService.Services;

public class ContactClient : IContactClient
{
  private readonly HttpClient _httpClient;
  private readonly ILogger<ContactClient> _logger;

  public ContactClient(HttpClient httpClient, ILogger<ContactClient> logger)
  {
    _httpClient = httpClient;
    _logger = logger;
  }

  public async Task<bool> ContactExistsAsync(Guid contactId)
  {
    try
    {
      var response = await _httpClient.GetAsync($"/api/contacts/{contactId}");
      return response.IsSuccessStatusCode;
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Failed to verify contact {ContactId} with ContactService. Failing open.", contactId);
      return true;
    }
  }
}
