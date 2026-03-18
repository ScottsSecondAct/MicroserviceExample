namespace AuthService.Services;

public interface IUsernameService
{
  Task<string> DeriveUniqueUsernameAsync(string email, Guid tenantId);
}
