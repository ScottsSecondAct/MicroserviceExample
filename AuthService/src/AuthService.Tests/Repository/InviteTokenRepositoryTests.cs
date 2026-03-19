using AuthService.Data;
using AuthService.Models;
using AuthService.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Tests.Repository;

public class InviteTokenRepositoryTests
{
    private static AuthDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task AddAsync_PersistsInviteToken()
    {
        using var ctx = CreateContext();
        var repo = new InviteTokenRepository(ctx);
        var token = new InviteToken
        {
            Id = Guid.NewGuid(),
            Token = "abc123",
            Email = "invite@example.com",
            ExpiresAt = DateTime.UtcNow.AddDays(2)
        };

        await repo.AddAsync(token);

        var stored = await ctx.InviteTokens.FindAsync(token.Id);
        stored.Should().NotBeNull();
        stored!.Token.Should().Be("abc123");
    }

    [Fact]
    public async Task GetByTokenAsync_WhenFound_ReturnsToken()
    {
        using var ctx = CreateContext();
        var token = new InviteToken
        {
            Id = Guid.NewGuid(),
            Token = "findme",
            Email = "user@example.com",
            ExpiresAt = DateTime.UtcNow.AddDays(2)
        };
        ctx.InviteTokens.Add(token);
        await ctx.SaveChangesAsync();
        var repo = new InviteTokenRepository(ctx);

        var result = await repo.GetByTokenAsync("findme");

        result.Should().NotBeNull();
        result!.Email.Should().Be("user@example.com");
    }

    [Fact]
    public async Task GetByTokenAsync_WhenNotFound_ReturnsNull()
    {
        using var ctx = CreateContext();
        var repo = new InviteTokenRepository(ctx);

        var result = await repo.GetByTokenAsync("does-not-exist");

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        using var ctx = CreateContext();
        var token = new InviteToken
        {
            Id = Guid.NewGuid(),
            Token = "updateme",
            Email = "user@example.com",
            ExpiresAt = DateTime.UtcNow.AddDays(2)
        };
        ctx.InviteTokens.Add(token);
        await ctx.SaveChangesAsync();
        var repo = new InviteTokenRepository(ctx);

        token.IsUsed = true;
        await repo.UpdateAsync(token);

        var stored = await ctx.InviteTokens.FindAsync(token.Id);
        stored!.IsUsed.Should().BeTrue();
    }
}
