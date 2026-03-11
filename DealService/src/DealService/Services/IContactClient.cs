namespace DealService.Services;

public interface IContactClient
{
  Task<bool> ContactExistsAsync(Guid contactId);
}
