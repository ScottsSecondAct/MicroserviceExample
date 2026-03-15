using AccountService.Models;
using AccountService.Models.DTOs;
using AccountService.Models.Enums;
using AccountService.Repository;
using AccountService.Services;
using FluentAssertions;
using MassTransit;
using Moq;
using SharedLibrary.Accounts.Events;

namespace AccountService.Tests.Services;

public class AccountsServiceTests
{
  private readonly Mock<IAccountRepository> _mockRepository;
  private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
  private readonly AccountsService _service;

  public AccountsServiceTests()
  {
    _mockRepository = new Mock<IAccountRepository>();
    _mockPublishEndpoint = new Mock<IPublishEndpoint>();
    _service = new AccountsService(_mockRepository.Object, _mockPublishEndpoint.Object);
  }

  [Fact]
  public async Task CreateAccountAsync_WithValidRequest_ReturnsSuccess()
  {
    var request = new CreateAccountRequest
    {
      Name = "Acme Corp",
      Industry = AccountIndustry.Technology,
      Size = AccountSize.Medium
    };

    _mockRepository.Setup(r => r.AddAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
    _mockPublishEndpoint
      .Setup(p => p.Publish(It.IsAny<AccountCreated>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    var result = await _service.CreateAccountAsync(request);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(201);
    var response = result.Data as AccountResponse;
    response.Should().NotBeNull();
    response!.Name.Should().Be("Acme Corp");
    response.Industry.Should().Be(AccountIndustry.Technology);
    _mockPublishEndpoint.Verify(
      p => p.Publish(It.IsAny<AccountCreated>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task GetAccountAsync_WhenFound_ReturnsSuccess()
  {
    var accountId = Guid.NewGuid();
    var account = new Account
    {
      AccountId = accountId,
      Name = "Test Corp",
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    _mockRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

    var result = await _service.GetAccountAsync(accountId);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(200);
    var response = result.Data as AccountResponse;
    response.Should().NotBeNull();
    response!.AccountId.Should().Be(accountId);
  }

  [Fact]
  public async Task GetAccountAsync_WhenNotFound_ReturnsFailure()
  {
    var accountId = Guid.NewGuid();
    _mockRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

    var result = await _service.GetAccountAsync(accountId);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(404);
  }

  [Fact]
  public async Task UpdateAccountAsync_WhenFound_ReturnsSuccess()
  {
    var accountId = Guid.NewGuid();
    var account = new Account
    {
      AccountId = accountId,
      Name = "Old Name",
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };
    var request = new UpdateAccountRequest { Name = "New Name" };

    _mockRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
    _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);

    var result = await _service.UpdateAccountAsync(accountId, request);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(200);
    var response = result.Data as AccountResponse;
    response.Should().NotBeNull();
    response!.Name.Should().Be("New Name");
  }

  [Fact]
  public async Task UpdateAccountAsync_WhenNotFound_ReturnsFailure()
  {
    var accountId = Guid.NewGuid();
    _mockRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

    var result = await _service.UpdateAccountAsync(accountId, new UpdateAccountRequest());

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(404);
  }

  [Fact]
  public async Task DeleteAccountAsync_WhenFound_ReturnsSuccess()
  {
    var accountId = Guid.NewGuid();
    var account = new Account
    {
      AccountId = accountId,
      Name = "To Delete",
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    _mockRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
    _mockRepository.Setup(r => r.DeleteAsync(accountId)).Returns(Task.CompletedTask);
    _mockPublishEndpoint
      .Setup(p => p.Publish(It.IsAny<AccountDeleted>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    var result = await _service.DeleteAccountAsync(accountId);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(204);
    _mockPublishEndpoint.Verify(
      p => p.Publish(It.IsAny<AccountDeleted>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task DeleteAccountAsync_WhenNotFound_ReturnsFailure()
  {
    var accountId = Guid.NewGuid();
    _mockRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

    var result = await _service.DeleteAccountAsync(accountId);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(404);
  }

  [Fact]
  public async Task UpdateAccountAsync_WhenAllOptionalFieldsProvided_UpdatesAll()
  {
    var accountId = Guid.NewGuid();
    var account = new Account
    {
      AccountId = accountId,
      Name = "Old Name",
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };
    var request = new UpdateAccountRequest
    {
      Industry = AccountIndustry.Technology,
      Size = AccountSize.Large,
      Website = "https://acme.com",
      Street = "123 Main St",
      City = "San Francisco",
      State = "CA",
      PostalCode = "94105",
      Country = "US"
    };

    _mockRepository.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
    _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);

    var result = await _service.UpdateAccountAsync(accountId, request);

    result.IsSuccess.Should().BeTrue();
    var response = result.Data as AccountResponse;
    response!.Industry.Should().Be(AccountIndustry.Technology);
    response.Size.Should().Be(AccountSize.Large);
    response.Website.Should().Be("https://acme.com");
    response.City.Should().Be("San Francisco");
  }

  [Fact]
  public async Task GetAllAccountsAsync_ReturnsAllAccounts()
  {
    var accounts = new List<Account>
    {
      new() { AccountId = Guid.NewGuid(), Name = "Corp A", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
      new() { AccountId = Guid.NewGuid(), Name = "Corp B", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
    };

    _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(accounts);

    var result = await _service.GetAllAccountsAsync();

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(200);
    var response = result.Data as List<AccountResponse>;
    response.Should().NotBeNull();
    response!.Count.Should().Be(2);
  }
}
