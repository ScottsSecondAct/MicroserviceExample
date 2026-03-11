using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Enums;
using UserManagementService.Data;
using UserManagementService.Models;
using UserManagementService.Repository;

namespace UserManagementService.Tests.Repository;

public class UserProfileRepositoryTests
{
  private static UserManagementDbContext CreateContext() =>
    new(new DbContextOptionsBuilder<UserManagementDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options);

  [Fact]
  public async Task AddAsync_PersistsProfile()
  {
    using var ctx = CreateContext();
    var repo = new UserProfileRepository(ctx);
    var profile = new UserProfile
    {
      UserId = Guid.NewGuid(),
      Email = "a@example.com",
      Role = UserRole.Member,
      DisplayName = "Alice"
    };

    await repo.AddAsync(profile);

    var stored = await ctx.UserProfiles.FindAsync(profile.UserId);
    stored.Should().NotBeNull();
    stored!.Email.Should().Be("a@example.com");
  }

  [Fact]
  public async Task GetByIdAsync_ReturnsProfile_WhenExists()
  {
    using var ctx = CreateContext();
    var id = Guid.NewGuid();
    ctx.UserProfiles.Add(new UserProfile { UserId = id, Email = "b@example.com", Role = UserRole.Member, DisplayName = "Bob" });
    await ctx.SaveChangesAsync();
    var repo = new UserProfileRepository(ctx);

    var result = await repo.GetByIdAsync(id);

    result.Should().NotBeNull();
    result!.UserId.Should().Be(id);
  }

  [Fact]
  public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
  {
    using var ctx = CreateContext();
    var repo = new UserProfileRepository(ctx);

    var result = await repo.GetByIdAsync(Guid.NewGuid());

    result.Should().BeNull();
  }

  [Fact]
  public async Task GetByEmailAsync_ReturnsProfile_WhenExists()
  {
    using var ctx = CreateContext();
    ctx.UserProfiles.Add(new UserProfile
    {
      UserId = Guid.NewGuid(),
      Email = "c@example.com",
      Role = UserRole.Member,
      DisplayName = "Carol"
    });
    await ctx.SaveChangesAsync();
    var repo = new UserProfileRepository(ctx);

    var result = await repo.GetByEmailAsync("c@example.com");

    result.Should().NotBeNull();
    result!.Email.Should().Be("c@example.com");
  }

  [Fact]
  public async Task GetByEmailAsync_ReturnsNull_WhenNotFound()
  {
    using var ctx = CreateContext();
    var repo = new UserProfileRepository(ctx);

    var result = await repo.GetByEmailAsync("missing@example.com");

    result.Should().BeNull();
  }

  [Fact]
  public async Task GetAllAsync_ReturnsAllProfiles()
  {
    using var ctx = CreateContext();
    ctx.UserProfiles.AddRange(
      new UserProfile { UserId = Guid.NewGuid(), Email = "d@example.com", Role = UserRole.Member, DisplayName = "Dave" },
      new UserProfile { UserId = Guid.NewGuid(), Email = "e@example.com", Role = UserRole.Admin, DisplayName = "Eve" }
    );
    await ctx.SaveChangesAsync();
    var repo = new UserProfileRepository(ctx);

    var result = await repo.GetAllAsync();

    result.Should().HaveCount(2);
  }

  [Fact]
  public async Task UpdateAsync_PersistsChanges()
  {
    using var ctx = CreateContext();
    var id = Guid.NewGuid();
    ctx.UserProfiles.Add(new UserProfile { UserId = id, Email = "f@example.com", Role = UserRole.Member, DisplayName = "Old Name" });
    await ctx.SaveChangesAsync();
    var repo = new UserProfileRepository(ctx);

    var profile = await ctx.UserProfiles.FindAsync(id);
    profile!.DisplayName = "New Name";
    await repo.UpdateAsync(profile);

    var updated = await ctx.UserProfiles.FindAsync(id);
    updated!.DisplayName.Should().Be("New Name");
  }

  [Fact]
  public async Task DeleteAsync_RemovesProfile()
  {
    using var ctx = CreateContext();
    var id = Guid.NewGuid();
    ctx.UserProfiles.Add(new UserProfile { UserId = id, Email = "g@example.com", Role = UserRole.Member, DisplayName = "Gone" });
    await ctx.SaveChangesAsync();
    var repo = new UserProfileRepository(ctx);

    await repo.DeleteAsync(id);

    var deleted = await ctx.UserProfiles.FindAsync(id);
    deleted.Should().BeNull();
  }

  [Fact]
  public async Task DeleteAsync_DoesNotThrow_WhenNotFound()
  {
    using var ctx = CreateContext();
    var repo = new UserProfileRepository(ctx);

    var act = async () => await repo.DeleteAsync(Guid.NewGuid());

    await act.Should().NotThrowAsync();
  }
}
