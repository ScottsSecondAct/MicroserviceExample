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

  [Fact]
  public async Task GetUserByUsernameAsync_ReturnsUser_WhenExistsInTenant()
  {
    using var ctx = CreateContext();
    var tenantId = Guid.NewGuid();
    ctx.Users.Add(new User { UserId = Guid.NewGuid(), Email = "f@example.com", Username = "johndoe", TenantId = tenantId, PasswordHash = "hash" });
    await ctx.SaveChangesAsync();
    var repo = new UserRepository(ctx);

    var result = await repo.GetUserByUsernameAsync(tenantId, "johndoe");

    result.Should().NotBeNull();
    result!.Username.Should().Be("johndoe");
  }

  [Fact]
  public async Task GetUserByUsernameAsync_ReturnsNull_WhenUsernameNotInTenant()
  {
    using var ctx = CreateContext();
    var tenantId = Guid.NewGuid();
    var otherTenantId = Guid.NewGuid();
    ctx.Users.Add(new User { UserId = Guid.NewGuid(), Email = "g@example.com", Username = "johndoe", TenantId = tenantId, PasswordHash = "hash" });
    await ctx.SaveChangesAsync();
    var repo = new UserRepository(ctx);

    var result = await repo.GetUserByUsernameAsync(otherTenantId, "johndoe");

    result.Should().BeNull();
  }

  [Fact]
  public async Task GetUserByUsernameAsync_ReturnsNull_WhenUsernameDoesNotExist()
  {
    using var ctx = CreateContext();
    var repo = new UserRepository(ctx);

    var result = await repo.GetUserByUsernameAsync(Guid.NewGuid(), "nobody");

    result.Should().BeNull();
  }

  [Fact]
  public async Task GetDefaultTenantIdAsync_ReturnsTenantId_WhenTenantExists()
  {
    using var ctx = CreateContext();
    var tenantId = Guid.NewGuid();
    ctx.Tenants.Add(new AuthService.Models.Tenant { TenantId = tenantId, Slug = "default", DisplayName = "Default", CreatedAt = DateTime.UtcNow });
    await ctx.SaveChangesAsync();
    var repo = new UserRepository(ctx);

    var result = await repo.GetDefaultTenantIdAsync();

    result.Should().Be(tenantId);
  }

  [Fact]
  public async Task GetDefaultTenantIdAsync_ReturnsGuidEmpty_WhenNoTenantsExist()
  {
    using var ctx = CreateContext();
    var repo = new UserRepository(ctx);

    var result = await repo.GetDefaultTenantIdAsync();

    result.Should().Be(Guid.Empty);
  }
}
