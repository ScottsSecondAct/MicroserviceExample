namespace ContactService.Services;

public interface IAccountClient
{
  Task<bool> AccountExistsAsync(Guid accountId);
}
