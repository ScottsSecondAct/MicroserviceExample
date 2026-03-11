using AuthService.Data;
using AuthService.Models;
using AuthService.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Tests.Repository;

public class UserRepositoryTests
{
  private static AuthDbContext CreateContext() =>
    new(new DbContextOptionsBuilder<AuthDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options);

  [Fact]
  public async Task AddUserAsync_PersistsUser()
  {
    using var ctx = CreateContext();
    var repo = new UserRepository(ctx);
    var user = new User { UserId = Guid.NewGuid(), Email = "a@example.com", PasswordHash = "hash" };

    await repo.AddUserAsync(user);

    var stored = await ctx.Users.FindAsync(user.UserId);
    stored.Should().NotBeNull();
    stored!.Email.Should().Be("a@example.com");
  }

  [Fact]
  public async Task GetUserByEmailAsync_ReturnsUser_WhenExists()
  {
    using var ctx = CreateContext();
    ctx.Users.Add(new User { UserId = Guid.NewGuid(), Email = "b@example.com", PasswordHash = "hash" });
    await ctx.SaveChangesAsync();
    var repo = new UserRepository(ctx);

    var result = await repo.GetUserByEmailAsync("b@example.com");

    result.Should().NotBeNull();
    result!.Email.Should().Be("b@example.com");
  }

  [Fact]
  public async Task GetUserByEmailAsync_ReturnsNull_WhenNotFound()
  {
    using var ctx = CreateContext();
    var repo = new UserRepository(ctx);

    var result = await repo.GetUserByEmailAsync("missing@example.com");

    result.Should().BeNull();
  }

  [Fact]
  public async Task GetUserByIdAsync_ReturnsUser_WhenExists()
  {
    using var ctx = CreateContext();
    var id = Guid.NewGuid();
    ctx.Users.Add(new User { UserId = id, Email = "c@example.com", PasswordHash = "hash" });
    await ctx.SaveChangesAsync();
    var repo = new UserRepository(ctx);

    var result = await repo.GetUserByIdAsync(id);

    result.Should().NotBeNull();
    result!.UserId.Should().Be(id);
  }

  [Fact]
  public async Task GetUserByIdAsync_ReturnsNull_WhenNotFound()
  {
    using var ctx = CreateContext();
    var repo = new UserRepository(ctx);

    var result = await repo.GetUserByIdAsync(Guid.NewGuid());

    result.Should().BeNull();
  }

  [Fact]
  public async Task UpdateUserAsync_PersistsChanges()
  {
    using var ctx = CreateContext();
    var id = Guid.NewGuid();
    ctx.Users.Add(new User { UserId = id, Email = "d@example.com", PasswordHash = "old_hash" });
    await ctx.SaveChangesAsync();
    var repo = new UserRepository(ctx);

    var user = await ctx.Users.FindAsync(id);
    user!.PasswordHash = "new_hash";
    await repo.UpdateUserAsync(user);

    var updated = await ctx.Users.FindAsync(id);
    updated!.PasswordHash.Should().Be("new_hash");
  }

  [Fact]
  public async Task DeleteUserAsync_RemovesUser()
  {
    using var ctx = CreateContext();
    var id = Guid.NewGuid();
    ctx.Users.Add(new User { UserId = id, Email = "e@example.com", PasswordHash = "hash" });
    await ctx.SaveChangesAsync();
    var repo = new UserRepository(ctx);

    await repo.DeleteUserAsync(id);

    var deleted = await ctx.Users.FindAsync(id);
    deleted.Should().BeNull();
  }

  [Fact]
  public async Task DeleteUserAsync_DoesNotThrow_WhenUserNotFound()
  {
    using var ctx = CreateContext();
    var repo = new UserRepository(ctx);

    var act = async () => await repo.DeleteUserAsync(Guid.NewGuid());

    await act.Should().NotThrowAsync();
  }
}
