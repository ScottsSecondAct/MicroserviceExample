using AccountService.Data;
using AccountService.Models;
using AccountService.Models.Enums;
using AccountService.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Tests.Repository;

public class AccountRepositoryTests
{
  private static AccountDbContext CreateContext() =>
    new(new DbContextOptionsBuilder<AccountDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options);

  private static Account MakeAccount(string name = "Acme Corp") =>
    new()
    {
      AccountId = Guid.NewGuid(),
      Name = name,
      Industry = AccountIndustry.Technology,
      Size = AccountSize.Medium,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

  // ── GetAllAsync ───────────────────────────────────────────────────────────

  [Fact]
  public async Task GetAllAsync_ReturnsAllAccounts()
  {
    using var ctx = CreateContext();
    ctx.Accounts.AddRange(MakeAccount("Alpha"), MakeAccount("Beta"), MakeAccount("Gamma"));
    await ctx.SaveChangesAsync();
    var repo = new AccountRepository(ctx);

    var result = await repo.GetAllAsync();

    result.Should().HaveCount(3);
  }

  [Fact]
  public async Task GetAllAsync_ReturnsEmptyList_WhenNoAccounts()
  {
    using var ctx = CreateContext();
    var repo = new AccountRepository(ctx);

    var result = await repo.GetAllAsync();

    result.Should().BeEmpty();
  }

  // ── GetByIdAsync ──────────────────────────────────────────────────────────

  [Fact]
  public async Task GetByIdAsync_ReturnsAccount_WhenExists()
  {
    using var ctx = CreateContext();
    var account = MakeAccount();
    ctx.Accounts.Add(account);
    await ctx.SaveChangesAsync();
    var repo = new AccountRepository(ctx);

    var result = await repo.GetByIdAsync(account.AccountId);

    result.Should().NotBeNull();
    result!.AccountId.Should().Be(account.AccountId);
    result.Name.Should().Be("Acme Corp");
  }

  [Fact]
  public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
  {
    using var ctx = CreateContext();
    var repo = new AccountRepository(ctx);

    var result = await repo.GetByIdAsync(Guid.NewGuid());

    result.Should().BeNull();
  }

  // ── AddAsync ──────────────────────────────────────────────────────────────

  [Fact]
  public async Task AddAsync_PersistsAccount()
  {
    using var ctx = CreateContext();
    var repo = new AccountRepository(ctx);
    var account = MakeAccount("New Corp");

    await repo.AddAsync(account);

    var stored = await ctx.Accounts.FindAsync(account.AccountId);
    stored.Should().NotBeNull();
    stored!.Name.Should().Be("New Corp");
  }

  // ── UpdateAsync ───────────────────────────────────────────────────────────

  [Fact]
  public async Task UpdateAsync_PersistsChanges()
  {
    using var ctx = CreateContext();
    var account = MakeAccount("Old Name");
    ctx.Accounts.Add(account);
    await ctx.SaveChangesAsync();
    var repo = new AccountRepository(ctx);

    account.Name = "New Name";
    account.Industry = AccountIndustry.Finance;
    await repo.UpdateAsync(account);

    var updated = await ctx.Accounts.FindAsync(account.AccountId);
    updated!.Name.Should().Be("New Name");
    updated.Industry.Should().Be(AccountIndustry.Finance);
  }

  // ── DeleteAsync ───────────────────────────────────────────────────────────

  [Fact]
  public async Task DeleteAsync_SoftDeletesAccount()
  {
    using var ctx = CreateContext();
    var account = MakeAccount();
    ctx.Accounts.Add(account);
    await ctx.SaveChangesAsync();
    var repo = new AccountRepository(ctx);

    await repo.DeleteAsync(account.AccountId);

    var softDeleted = await ctx.Accounts.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.AccountId == account.AccountId);
    softDeleted.Should().NotBeNull();
    softDeleted!.IsDeleted.Should().BeTrue();
    softDeleted.DeletedAt.Should().NotBeNull();
  }

  [Fact]
  public async Task DeleteAsync_ExcludesAccountFromQueries()
  {
    using var ctx = CreateContext();
    var account = MakeAccount();
    ctx.Accounts.Add(account);
    await ctx.SaveChangesAsync();
    var repo = new AccountRepository(ctx);

    await repo.DeleteAsync(account.AccountId);

    var found = await repo.GetByIdAsync(account.AccountId);
    found.Should().BeNull();
    var all = await repo.GetAllAsync();
    all.Should().NotContain(a => a.AccountId == account.AccountId);
  }

  [Fact]
  public async Task DeleteAsync_DoesNotThrow_WhenNotFound()
  {
    using var ctx = CreateContext();
    var repo = new AccountRepository(ctx);

    var act = async () => await repo.DeleteAsync(Guid.NewGuid());

    await act.Should().NotThrowAsync();
  }

  // ── AuditLog ──────────────────────────────────────────────────────────────

  [Fact]
  public async Task AddAsync_CreatesAuditLogEntry()
  {
    using var ctx = CreateContext();
    var repo = new AccountRepository(ctx);
    var account = MakeAccount("Audit Corp");

    await repo.AddAsync(account);

    var auditLogs = await ctx.AuditLogs.Where(a => a.EntityId == account.AccountId.ToString()).ToListAsync();
    auditLogs.Should().ContainSingle(a => a.Action == "Created");
  }
}
