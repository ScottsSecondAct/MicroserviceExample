using AccountService.Models;
using AccountService.Models.DTOs;
using AccountService.Repository;
using MassTransit;
using SharedLibrary.Accounts.Events;

namespace AccountService.Services;

public class AccountsService : IAccountService
{
  private readonly IAccountRepository _repository;
  private readonly IPublishEndpoint _publishEndpoint;

  public AccountsService(IAccountRepository repository, IPublishEndpoint publishEndpoint)
  {
    _repository = repository;
    _publishEndpoint = publishEndpoint;
  }

  public async Task<ServiceResult> GetAllAccountsAsync()
  {
    var accounts = await _repository.GetAllAsync();
    var response = accounts.Select(MapToResponse).ToList();
    return ServiceResult.Success(response);
  }

  public async Task<ServiceResult> GetAccountAsync(Guid id)
  {
    var account = await _repository.GetByIdAsync(id);
    if (account == null)
      return ServiceResult.Failure("Account not found.", 404);

    return ServiceResult.Success(MapToResponse(account));
  }

  public async Task<ServiceResult> CreateAccountAsync(CreateAccountRequest request)
  {
    var account = new Account
    {
      AccountId = Guid.NewGuid(),
      Name = request.Name,
      Industry = request.Industry,
      Size = request.Size,
      Website = request.Website,
      Street = request.Street,
      City = request.City,
      State = request.State,
      PostalCode = request.PostalCode,
      Country = request.Country,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    await _repository.AddAsync(account);

    await _publishEndpoint.Publish(new AccountCreated
    {
      AccountId = account.AccountId,
      Name = account.Name
    });

    return ServiceResult.Success(MapToResponse(account), "Account created successfully.", 201);
  }

  public async Task<ServiceResult> UpdateAccountAsync(Guid id, UpdateAccountRequest request)
  {
    var account = await _repository.GetByIdAsync(id);
    if (account == null)
      return ServiceResult.Failure("Account not found.", 404);

    if (request.Name != null) account.Name = request.Name;
    if (request.Industry.HasValue) account.Industry = request.Industry;
    if (request.Size.HasValue) account.Size = request.Size;
    if (request.Website != null) account.Website = request.Website;
    if (request.Street != null) account.Street = request.Street;
    if (request.City != null) account.City = request.City;
    if (request.State != null) account.State = request.State;
    if (request.PostalCode != null) account.PostalCode = request.PostalCode;
    if (request.Country != null) account.Country = request.Country;
    account.UpdatedAt = DateTime.UtcNow;

    await _repository.UpdateAsync(account);

    return ServiceResult.Success(MapToResponse(account));
  }

  public async Task<ServiceResult> DeleteAccountAsync(Guid id)
  {
    var account = await _repository.GetByIdAsync(id);
    if (account == null)
      return ServiceResult.Failure("Account not found.", 404);

    await _repository.DeleteAsync(id);

    await _publishEndpoint.Publish(new AccountDeleted
    {
      AccountId = id
    });

    return ServiceResult.Success(statusCode: 204);
  }

  private static AccountResponse MapToResponse(Account account) => new()
  {
    AccountId = account.AccountId,
    Name = account.Name,
    Industry = account.Industry,
    Size = account.Size,
    Website = account.Website,
    Street = account.Street,
    City = account.City,
    State = account.State,
    PostalCode = account.PostalCode,
    Country = account.Country,
    CreatedAt = account.CreatedAt,
    UpdatedAt = account.UpdatedAt
  };
}
