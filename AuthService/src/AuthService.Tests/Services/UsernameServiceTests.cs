using AuthService.Models;
using AuthService.Repository;
using AuthService.Services;
using FluentAssertions;
using Moq;

namespace AuthService.Tests.Services;

public class UsernameServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static UsernameService CreateService(Mock<IUserRepository> repo) =>
        new(repo.Object);

    [Fact]
    public async Task DeriveUniqueUsernameAsync_WhenUsernameAvailable_ReturnsPrefixDirectly()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetUserByUsernameAsync(TenantId, "alice"))
            .ReturnsAsync((User?)null);

        var service = CreateService(repo);
        var result = await service.DeriveUniqueUsernameAsync("alice@example.com", TenantId);

        result.Should().Be("alice");
    }

    [Fact]
    public async Task DeriveUniqueUsernameAsync_WhenUsernameCollides_AppendsNumericSuffix()
    {
        var repo = new Mock<IUserRepository>();
        // "alice" is taken, "alice2" is free
        repo.Setup(r => r.GetUserByUsernameAsync(TenantId, "alice"))
            .ReturnsAsync(new User { UserId = Guid.NewGuid(), Email = "other@example.com" });
        repo.Setup(r => r.GetUserByUsernameAsync(TenantId, "alice2"))
            .ReturnsAsync((User?)null);

        var service = CreateService(repo);
        var result = await service.DeriveUniqueUsernameAsync("alice@example.com", TenantId);

        result.Should().Be("alice2");
    }

    [Fact]
    public async Task DeriveUniqueUsernameAsync_WhenMultipleCollisions_IncreasesSuffix()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetUserByUsernameAsync(TenantId, "bob"))
            .ReturnsAsync(new User { UserId = Guid.NewGuid(), Email = "a@example.com" });
        repo.Setup(r => r.GetUserByUsernameAsync(TenantId, "bob2"))
            .ReturnsAsync(new User { UserId = Guid.NewGuid(), Email = "b@example.com" });
        repo.Setup(r => r.GetUserByUsernameAsync(TenantId, "bob3"))
            .ReturnsAsync((User?)null);

        var service = CreateService(repo);
        var result = await service.DeriveUniqueUsernameAsync("bob@example.com", TenantId);

        result.Should().Be("bob3");
    }

    [Fact]
    public async Task DeriveUniqueUsernameAsync_WithDotsAndPlusInEmail_NormalizesPrefix()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetUserByUsernameAsync(TenantId, "john_doe_tag"))
            .ReturnsAsync((User?)null);

        var service = CreateService(repo);
        var result = await service.DeriveUniqueUsernameAsync("John.Doe+tag@example.com", TenantId);

        result.Should().Be("john_doe_tag");
    }
}
