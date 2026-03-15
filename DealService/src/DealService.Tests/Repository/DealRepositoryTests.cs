using DealService.Data;
using DealService.Models;
using DealService.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Deals.Enums;

namespace DealService.Tests.Repository;

public class DealRepositoryTests
{
  private static DealDbContext CreateContext() =>
    new(new DbContextOptionsBuilder<DealDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options);

  private static Deal MakeDeal(
    DealStage stage = DealStage.Prospecting,
    Guid? accountId = null,
    Guid? ownerId = null) =>
    new()
    {
      DealId = Guid.NewGuid(),
      Title = "Test Deal",
      Stage = stage,
      AccountId = accountId,
      OwnerId = ownerId,
      Value = 10000m,
      Probability = 50,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

  // ── GetAllAsync (filters) ─────────────────────────────────────────────────

  [Fact]
  public async Task GetAllAsync_ReturnsAllDeals()
  {
    using var ctx = CreateContext();
    ctx.Deals.AddRange(MakeDeal(DealStage.Prospecting), MakeDeal(DealStage.Proposal));
    await ctx.SaveChangesAsync();
    var repo = new DealRepository(ctx);

    var result = await repo.GetAllAsync();

    result.Should().HaveCount(2);
  }

  [Fact]
  public async Task GetAllAsync_FiltersByStage()
  {
    using var ctx = CreateContext();
    ctx.Deals.AddRange(
      MakeDeal(DealStage.Prospecting),
      MakeDeal(DealStage.Prospecting),
      MakeDeal(DealStage.Proposal));
    await ctx.SaveChangesAsync();
    var repo = new DealRepository(ctx);

    var result = await repo.GetAllAsync(stage: DealStage.Prospecting);

    result.Should().HaveCount(2);
    result.Should().OnlyContain(d => d.Stage == DealStage.Prospecting);
  }

  [Fact]
  public async Task GetAllAsync_FiltersByAccountId()
  {
    using var ctx = CreateContext();
    var accountId = Guid.NewGuid();
    ctx.Deals.AddRange(
      MakeDeal(accountId: accountId),
      MakeDeal(accountId: accountId),
      MakeDeal(accountId: Guid.NewGuid()));
    await ctx.SaveChangesAsync();
    var repo = new DealRepository(ctx);

    var result = await repo.GetAllAsync(accountId: accountId);

    result.Should().HaveCount(2);
    result.Should().OnlyContain(d => d.AccountId == accountId);
  }

  [Fact]
  public async Task GetAllAsync_FiltersByOwnerId()
  {
    using var ctx = CreateContext();
    var ownerId = Guid.NewGuid();
    ctx.Deals.AddRange(
      MakeDeal(ownerId: ownerId),
      MakeDeal(ownerId: Guid.NewGuid()),
      MakeDeal(ownerId: Guid.NewGuid()));
    await ctx.SaveChangesAsync();
    var repo = new DealRepository(ctx);

    var result = await repo.GetAllAsync(ownerId: ownerId);

    result.Should().HaveCount(1);
    result[0].OwnerId.Should().Be(ownerId);
  }

  // ── GetByIdAsync ──────────────────────────────────────────────────────────

  [Fact]
  public async Task GetByIdAsync_WhenFound_ReturnsDeal()
  {
    using var ctx = CreateContext();
    var deal = MakeDeal();
    ctx.Deals.Add(deal);
    await ctx.SaveChangesAsync();
    var repo = new DealRepository(ctx);

    var result = await repo.GetByIdAsync(deal.DealId);

    result.Should().NotBeNull();
    result!.DealId.Should().Be(deal.DealId);
  }

  [Fact]
  public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
  {
    using var ctx = CreateContext();
    var repo = new DealRepository(ctx);

    var result = await repo.GetByIdAsync(Guid.NewGuid());

    result.Should().BeNull();
  }

  // ── AddAsync ──────────────────────────────────────────────────────────────

  [Fact]
  public async Task AddAsync_AddsDealToDb()
  {
    using var ctx = CreateContext();
    var repo = new DealRepository(ctx);
    var deal = MakeDeal();

    await repo.AddAsync(deal);

    var stored = await ctx.Deals.FindAsync(deal.DealId);
    stored.Should().NotBeNull();
    stored!.Title.Should().Be("Test Deal");
  }

  // ── UpdateAsync ───────────────────────────────────────────────────────────

  [Fact]
  public async Task UpdateAsync_UpdatesDealInDb()
  {
    using var ctx = CreateContext();
    var deal = MakeDeal(DealStage.Prospecting);
    ctx.Deals.Add(deal);
    await ctx.SaveChangesAsync();
    var repo = new DealRepository(ctx);

    deal.Stage = DealStage.Proposal;
    deal.Title = "Updated Deal";
    await repo.UpdateAsync(deal);

    var updated = await ctx.Deals.FindAsync(deal.DealId);
    updated!.Stage.Should().Be(DealStage.Proposal);
    updated.Title.Should().Be("Updated Deal");
  }

  // ── DeleteAsync ───────────────────────────────────────────────────────────

  [Fact]
  public async Task DeleteAsync_SoftDeletesDeal()
  {
    using var ctx = CreateContext();
    var deal = MakeDeal();
    ctx.Deals.Add(deal);
    await ctx.SaveChangesAsync();
    var repo = new DealRepository(ctx);

    await repo.DeleteAsync(deal.DealId);

    var softDeleted = await ctx.Deals.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.DealId == deal.DealId);
    softDeleted.Should().NotBeNull();
    softDeleted!.IsDeleted.Should().BeTrue();
    softDeleted.DeletedAt.Should().NotBeNull();
  }

  [Fact]
  public async Task DeleteAsync_ExcludesDealFromQueries()
  {
    using var ctx = CreateContext();
    var deal = MakeDeal();
    ctx.Deals.Add(deal);
    await ctx.SaveChangesAsync();
    var repo = new DealRepository(ctx);

    await repo.DeleteAsync(deal.DealId);

    var found = await repo.GetByIdAsync(deal.DealId);
    found.Should().BeNull();
    var all = await repo.GetAllAsync();
    all.Should().NotContain(d => d.DealId == deal.DealId);
  }

  [Fact]
  public async Task DeleteAsync_WhenNotFound_DoesNotThrow()
  {
    using var ctx = CreateContext();
    var repo = new DealRepository(ctx);

    var act = async () => await repo.DeleteAsync(Guid.NewGuid());

    await act.Should().NotThrowAsync();
  }

  // ── AuditLog ──────────────────────────────────────────────────────────────

  [Fact]
  public async Task AddAsync_CreatesAuditLogEntry()
  {
    using var ctx = CreateContext();
    var repo = new DealRepository(ctx);
    var deal = MakeDeal();

    await repo.AddAsync(deal);

    var auditLogs = await ctx.AuditLogs.Where(a => a.EntityId == deal.DealId.ToString()).ToListAsync();
    auditLogs.Should().ContainSingle(a => a.Action == "Created");
  }

  // ── DealContact operations ─────────────────────────────────────────────────

  [Fact]
  public async Task AddDealContactAsync_AddsAssociation()
  {
    using var ctx = CreateContext();
    var deal = MakeDeal();
    ctx.Deals.Add(deal);
    await ctx.SaveChangesAsync();
    var repo = new DealRepository(ctx);

    var contactId = Guid.NewGuid();
    var dealContact = new DealContact
    {
      DealContactId = Guid.NewGuid(),
      DealId = deal.DealId,
      ContactId = contactId,
      Role = DealContactRole.DecisionMaker
    };

    await repo.AddDealContactAsync(dealContact);

    var stored = await ctx.DealContacts.FindAsync(dealContact.DealContactId);
    stored.Should().NotBeNull();
    stored!.ContactId.Should().Be(contactId);
  }

  [Fact]
  public async Task RemoveDealContactAsync_WhenFound_RemovesAssociation()
  {
    using var ctx = CreateContext();
    var deal = MakeDeal();
    ctx.Deals.Add(deal);
    await ctx.SaveChangesAsync();
    var dc = new DealContact
    {
      DealContactId = Guid.NewGuid(),
      DealId = deal.DealId,
      ContactId = Guid.NewGuid(),
      Role = DealContactRole.DecisionMaker
    };
    ctx.DealContacts.Add(dc);
    await ctx.SaveChangesAsync();
    var repo = new DealRepository(ctx);

    await repo.RemoveDealContactAsync(dc.DealContactId);

    var remaining = await ctx.DealContacts.FindAsync(dc.DealContactId);
    remaining.Should().BeNull();
  }

  [Fact]
  public async Task RemoveDealContactAsync_WhenNotFound_DoesNotThrow()
  {
    using var ctx = CreateContext();
    var repo = new DealRepository(ctx);

    var act = async () => await repo.RemoveDealContactAsync(Guid.NewGuid());

    await act.Should().NotThrowAsync();
  }

  [Fact]
  public async Task RemoveDealContactsByContactIdAsync_WhenNoMatches_DoesNotThrow()
  {
    using var ctx = CreateContext();
    var repo = new DealRepository(ctx);

    var act = async () => await repo.RemoveDealContactsByContactIdAsync(Guid.NewGuid());

    await act.Should().NotThrowAsync();
  }

  [Fact]
  public async Task RemoveDealContactsByContactIdAsync_RemovesAllForContact()
  {
    using var ctx = CreateContext();
    var deal1 = MakeDeal();
    var deal2 = MakeDeal();
    ctx.Deals.AddRange(deal1, deal2);
    await ctx.SaveChangesAsync();

    var contactId = Guid.NewGuid();
    ctx.DealContacts.AddRange(
      new DealContact { DealContactId = Guid.NewGuid(), DealId = deal1.DealId, ContactId = contactId, Role = DealContactRole.DecisionMaker },
      new DealContact { DealContactId = Guid.NewGuid(), DealId = deal2.DealId, ContactId = contactId, Role = DealContactRole.Influencer },
      new DealContact { DealContactId = Guid.NewGuid(), DealId = deal1.DealId, ContactId = Guid.NewGuid(), Role = DealContactRole.Champion }
    );
    await ctx.SaveChangesAsync();
    var repo = new DealRepository(ctx);

    await repo.RemoveDealContactsByContactIdAsync(contactId);

    var remaining = await ctx.DealContacts.Where(dc => dc.ContactId == contactId).ToListAsync();
    remaining.Should().BeEmpty();
    var others = await ctx.DealContacts.ToListAsync();
    others.Should().HaveCount(1);
  }
}
