using ContactService.Data;
using ContactService.Models;
using ContactService.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contacts.Enums;

namespace ContactService.Tests.Repository;

public class ContactRepositoryTests
{
  private static ContactDbContext CreateContext() =>
    new(new DbContextOptionsBuilder<ContactDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options);

  private static Contact MakeContact(
    ContactStatus status = ContactStatus.Lead,
    Guid? ownerId = null,
    Guid? accountId = null) =>
    new()
    {
      ContactId = Guid.NewGuid(),
      FirstName = "Jane",
      LastName = "Doe",
      Email = "jane@example.com",
      Status = status,
      OwnerId = ownerId,
      AccountId = accountId,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

  // ── GetAllAsync (filters) ─────────────────────────────────────────────────

  [Fact]
  public async Task GetAllAsync_NoFilter_ReturnsAllContacts()
  {
    using var ctx = CreateContext();
    ctx.Contacts.AddRange(MakeContact(ContactStatus.Lead), MakeContact(ContactStatus.Customer));
    await ctx.SaveChangesAsync();
    var repo = new ContactRepository(ctx);

    var result = await repo.GetAllAsync();

    result.Should().HaveCount(2);
  }

  [Fact]
  public async Task GetAllAsync_StatusFilter_ReturnsMatchingContacts()
  {
    using var ctx = CreateContext();
    ctx.Contacts.AddRange(
      MakeContact(ContactStatus.Lead),
      MakeContact(ContactStatus.Lead),
      MakeContact(ContactStatus.Customer)
    );
    await ctx.SaveChangesAsync();
    var repo = new ContactRepository(ctx);

    var result = await repo.GetAllAsync(status: ContactStatus.Lead);

    result.Should().HaveCount(2);
    result.Should().OnlyContain(c => c.Status == ContactStatus.Lead);
  }

  [Fact]
  public async Task GetAllAsync_OwnerIdFilter_ReturnsMatchingContacts()
  {
    using var ctx = CreateContext();
    var ownerId = Guid.NewGuid();
    ctx.Contacts.AddRange(
      MakeContact(ownerId: ownerId),
      MakeContact(ownerId: ownerId),
      MakeContact(ownerId: Guid.NewGuid())
    );
    await ctx.SaveChangesAsync();
    var repo = new ContactRepository(ctx);

    var result = await repo.GetAllAsync(ownerId: ownerId);

    result.Should().HaveCount(2);
    result.Should().OnlyContain(c => c.OwnerId == ownerId);
  }

  [Fact]
  public async Task GetAllAsync_AccountIdFilter_ReturnsMatchingContacts()
  {
    using var ctx = CreateContext();
    var accountId = Guid.NewGuid();
    ctx.Contacts.AddRange(
      MakeContact(accountId: accountId),
      MakeContact(accountId: Guid.NewGuid())
    );
    await ctx.SaveChangesAsync();
    var repo = new ContactRepository(ctx);

    var result = await repo.GetAllAsync(accountId: accountId);

    result.Should().HaveCount(1);
    result[0].AccountId.Should().Be(accountId);
  }

  // ── GetByIdAsync ──────────────────────────────────────────────────────────

  [Fact]
  public async Task GetByIdAsync_ReturnsContact_WhenExists()
  {
    using var ctx = CreateContext();
    var contact = MakeContact();
    ctx.Contacts.Add(contact);
    await ctx.SaveChangesAsync();
    var repo = new ContactRepository(ctx);

    var result = await repo.GetByIdAsync(contact.ContactId);

    result.Should().NotBeNull();
    result!.ContactId.Should().Be(contact.ContactId);
  }

  [Fact]
  public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
  {
    using var ctx = CreateContext();
    var repo = new ContactRepository(ctx);

    var result = await repo.GetByIdAsync(Guid.NewGuid());

    result.Should().BeNull();
  }

  // ── AddAsync ──────────────────────────────────────────────────────────────

  [Fact]
  public async Task AddAsync_PersistsContact()
  {
    using var ctx = CreateContext();
    var repo = new ContactRepository(ctx);
    var contact = MakeContact();

    await repo.AddAsync(contact);

    var stored = await ctx.Contacts.FindAsync(contact.ContactId);
    stored.Should().NotBeNull();
    stored!.Email.Should().Be("jane@example.com");
  }

  // ── UpdateAsync ───────────────────────────────────────────────────────────

  [Fact]
  public async Task UpdateAsync_PersistsChanges()
  {
    using var ctx = CreateContext();
    var contact = MakeContact(ContactStatus.Lead);
    ctx.Contacts.Add(contact);
    await ctx.SaveChangesAsync();
    var repo = new ContactRepository(ctx);

    contact.Status = ContactStatus.Prospect;
    await repo.UpdateAsync(contact);

    var updated = await ctx.Contacts.FindAsync(contact.ContactId);
    updated!.Status.Should().Be(ContactStatus.Prospect);
  }

  // ── DeleteAsync ───────────────────────────────────────────────────────────

  [Fact]
  public async Task DeleteAsync_RemovesContact()
  {
    using var ctx = CreateContext();
    var contact = MakeContact();
    ctx.Contacts.Add(contact);
    await ctx.SaveChangesAsync();
    var repo = new ContactRepository(ctx);

    await repo.DeleteAsync(contact.ContactId);

    var deleted = await ctx.Contacts.FindAsync(contact.ContactId);
    deleted.Should().BeNull();
  }

  [Fact]
  public async Task DeleteAsync_DoesNotThrow_WhenNotFound()
  {
    using var ctx = CreateContext();
    var repo = new ContactRepository(ctx);

    var act = async () => await repo.DeleteAsync(Guid.NewGuid());

    await act.Should().NotThrowAsync();
  }
}
