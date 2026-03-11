using AccountService.Models;

namespace AccountService.Repository;

public interface IAccountRepository
{
  Task<Account?> GetByIdAsync(Guid id);
  Task<List<Account>> GetAllAsync();
  Task AddAsync(Account account);
  Task UpdateAsync(Account account);
  Task DeleteAsync(Guid id);
}
