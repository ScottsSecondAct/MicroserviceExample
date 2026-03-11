using AccountService.Models.DTOs;

namespace AccountService.Services;

public interface IAccountService
{
  Task<ServiceResult> GetAllAccountsAsync();
  Task<ServiceResult> GetAccountAsync(Guid id);
  Task<ServiceResult> CreateAccountAsync(CreateAccountRequest request);
  Task<ServiceResult> UpdateAccountAsync(Guid id, UpdateAccountRequest request);
  Task<ServiceResult> DeleteAccountAsync(Guid id);
}
