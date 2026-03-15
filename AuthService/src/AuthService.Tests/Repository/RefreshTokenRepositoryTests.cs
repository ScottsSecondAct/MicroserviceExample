using AuthService.Data;
using AuthService.Models;
using AuthService.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Tests.Repository;

public class RefreshTokenRepositoryTests
{
  private static AuthDbContext CreateContext() =>
    new(new DbContextOptionsBuilder<AuthDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options);

  private static async Task<(AuthDbContext ctx, User user)> CreateContextWithUser()
  {
    var ctx = CreateContext();
    var user = new User { UserId = Guid.NewGuid(), Email = "a@example.com", PasswordHash = "hash" };
    ctx.Users.Add(user);
    await ctx.SaveChangesAsync();
    return (ctx, user);
  }

  [Fact]
  public async Task AddAsync_PersistsToken()
  {
    var (ctx, user) = await CreateContextWithUser();
    using var _ = ctx;
    var repo = new RefreshTokenRepository(ctx);
    var token = new RefreshToken
    {
      Id = Guid.NewGuid(),
      Token = "tok-123",
      UserId = user.UserId,
      ExpiresAt = DateTime.UtcNow.AddDays(7),
      IsRevoked = false
    };

    await repo.AddAsync(token);

    var stored = await ctx.RefreshTokens.FindAsync(token.Id);
    stored.Should().NotBeNull();
    stored!.Token.Should().Be("tok-123");
  }

  [Fact]
  public async Task GetByTokenAsync_ReturnsToken_WhenExists()
  {
    var (ctx, user) = await CreateContextWithUser();
    using var _ = ctx;
    var token = new RefreshToken
    {
      Id = Guid.NewGuid(),
      Token = "tok-found",
      UserId = user.UserId,
      ExpiresAt = DateTime.UtcNow.AddDays(7),
      IsRevoked = false
    };
    ctx.RefreshTokens.Add(token);
    await ctx.SaveChangesAsync();
    var repo = new RefreshTokenRepository(ctx);

    var result = await repo.GetByTokenAsync("tok-found");

    result.Should().NotBeNull();
    result!.Token.Should().Be("tok-found");
    result.User.Should().NotBeNull();
  }

  [Fact]
  public async Task GetByTokenAsync_ReturnsNull_WhenNotFound()
  {
    var (ctx, _) = await CreateContextWithUser();
    using var __ = ctx;
    var repo = new RefreshTokenRepository(ctx);

    var result = await repo.GetByTokenAsync("nonexistent");

    result.Should().BeNull();
  }

  [Fact]
  public async Task RevokeAsync_SetsIsRevoked_True()
  {
    var (ctx, user) = await CreateContextWithUser();
    using var _ = ctx;
    var token = new RefreshToken
    {
      Id = Guid.NewGuid(),
      Token = "tok-revoke",
      UserId = user.UserId,
      ExpiresAt = DateTime.UtcNow.AddDays(7),
      IsRevoked = false
    };
    ctx.RefreshTokens.Add(token);
    await ctx.SaveChangesAsync();
    var repo = new RefreshTokenRepository(ctx);

    await repo.RevokeAsync(token);

    var updated = await ctx.RefreshTokens.FindAsync(token.Id);
    updated!.IsRevoked.Should().BeTrue();
  }
}
