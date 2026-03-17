using AuthService.Data;
using AuthService.Models;
using AuthService.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Tests.Repository;

public class PasswordResetTokenRepositoryTests
{
  private static AuthDbContext CreateContext() =>
    new(new DbContextOptionsBuilder<AuthDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options);

  [Fact]
  public async Task AddAsync_PersistsToken()
  {
    using var ctx = CreateContext();
    var repo = new PasswordResetTokenRepository(ctx);
    var token = new PasswordResetToken
    {
      Id = Guid.NewGuid(),
      Token = "reset-token-abc",
      UserId = Guid.NewGuid(),
      Email = "user@example.com",
      ExpiresAt = DateTime.UtcNow.AddHours(1),
      IsUsed = false
    };

    await repo.AddAsync(token);

    var stored = await ctx.PasswordResetTokens.FindAsync(token.Id);
    stored.Should().NotBeNull();
    stored!.Token.Should().Be("reset-token-abc");
  }

  [Fact]
  public async Task GetByTokenAsync_ReturnsToken_WhenExists()
  {
    using var ctx = CreateContext();
    var repo = new PasswordResetTokenRepository(ctx);
    var token = new PasswordResetToken
    {
      Id = Guid.NewGuid(),
      Token = "find-me-token",
      UserId = Guid.NewGuid(),
      Email = "user@example.com",
      ExpiresAt = DateTime.UtcNow.AddHours(1),
      IsUsed = false
    };
    ctx.PasswordResetTokens.Add(token);
    await ctx.SaveChangesAsync();

    var result = await repo.GetByTokenAsync("find-me-token");

    result.Should().NotBeNull();
    result!.Email.Should().Be("user@example.com");
  }

  [Fact]
  public async Task GetByTokenAsync_ReturnsNull_WhenNotFound()
  {
    using var ctx = CreateContext();
    var repo = new PasswordResetTokenRepository(ctx);

    var result = await repo.GetByTokenAsync("nonexistent-token");

    result.Should().BeNull();
  }

  [Fact]
  public async Task UpdateAsync_PersistsChanges()
  {
    using var ctx = CreateContext();
    var repo = new PasswordResetTokenRepository(ctx);
    var token = new PasswordResetToken
    {
      Id = Guid.NewGuid(),
      Token = "update-me-token",
      UserId = Guid.NewGuid(),
      Email = "user@example.com",
      ExpiresAt = DateTime.UtcNow.AddHours(1),
      IsUsed = false
    };
    ctx.PasswordResetTokens.Add(token);
    await ctx.SaveChangesAsync();

    token.IsUsed = true;
    await repo.UpdateAsync(token);

    var stored = await ctx.PasswordResetTokens.FindAsync(token.Id);
    stored!.IsUsed.Should().BeTrue();
  }
}
