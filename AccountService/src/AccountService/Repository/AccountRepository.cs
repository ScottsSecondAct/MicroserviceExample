using AccountService.Data;
using AccountService.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Repository;

public class AccountRepository : IAccountRepository
{
  private readonly AccountDbContext _context;

  public AccountRepository(AccountDbContext context)
  {
    _context = context;
  }

  public async Task<Account?> GetByIdAsync(Guid id) =>
    await _context.Accounts.FindAsync(id);

  public async Task<List<Account>> GetAllAsync() =>
    await _context.Accounts.ToListAsync();

  public async Task AddAsync(Account account)
  {
    _context.Accounts.Add(account);
    await _context.SaveChangesAsync();
  }

  public async Task UpdateAsync(Account account)
  {
    _context.Accounts.Update(account);
    await _context.SaveChangesAsync();
  }

  public async Task DeleteAsync(Guid id)
  {
    var account = await _context.Accounts.FindAsync(id);
    if (account != null)
    {
      _context.Accounts.Remove(account);
      await _context.SaveChangesAsync();
    }
  }
}
