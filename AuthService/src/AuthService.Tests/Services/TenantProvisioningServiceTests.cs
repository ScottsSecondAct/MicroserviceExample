using AuthService.Data;
using AuthService.Models.DTOs;
using AuthService.Services;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SharedLibrary.Messaging.Events;

namespace AuthService.Tests.Services;

public class TenantProvisioningServiceTests
{
    private static AuthDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static TenantProvisioningService CreateService(
        AuthDbContext db,
        Mock<IPasswordPolicyService>? policyMock = null,
        Mock<IPublishEndpoint>? publishMock = null)
    {
        var passwordService = new Mock<IPasswordService>();
        passwordService.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed");

        if (policyMock == null)
        {
            policyMock = new Mock<IPasswordPolicyService>();
            policyMock.Setup(p => p.Validate(It.IsAny<string>()))
                .Returns((true, Array.Empty<string>()));
        }

        if (publishMock == null)
        {
            publishMock = new Mock<IPublishEndpoint>();
            publishMock.Setup(p => p.Publish(It.IsAny<UserRegistered>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        return new TenantProvisioningService(db, passwordService.Object, policyMock.Object, publishMock.Object);
    }

    private static ProvisionTenantRequest ValidRequest(string slug = "acme") => new()
    {
        Slug = slug,
        DisplayName = "Acme Corp",
        AdminEmail = "admin@acme.com",
        AdminPassword = "SecurePass1!",
        AdminUsername = "acme-admin"
    };

    [Fact]
    public async Task ProvisionAsync_WithInvalidPassword_ReturnsFailure400()
    {
        using var db = CreateDb();
        var policy = new Mock<IPasswordPolicyService>();
        policy.Setup(p => p.Validate(It.IsAny<string>()))
            .Returns((false, new[] { "Password too short.", "Needs a number." }));

        var service = CreateService(db, policyMock: policy);
        var request = ValidRequest();
        request.AdminPassword = "weak";

        var result = await service.ProvisionAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Contain("Password too short");
    }

    [Fact]
    public async Task ProvisionAsync_WithDuplicateSlug_ReturnsConflict409()
    {
        using var db = CreateDb();
        db.Tenants.Add(new AuthService.Models.Tenant
        {
            TenantId = Guid.NewGuid(),
            Slug = "acme",
            DisplayName = "Existing Corp",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.ProvisionAsync(ValidRequest("acme"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Message.Should().Contain("acme");
    }

    [Fact]
    public async Task ProvisionAsync_WithValidRequest_CreatesTenantAndAdminUser()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.ProvisionAsync(ValidRequest("newco"));

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        db.Tenants.Should().ContainSingle(t => t.Slug == "newco");
        db.Users.Should().ContainSingle(u => u.Email == "admin@acme.com");
    }

    [Fact]
    public async Task ProvisionAsync_WithValidRequest_PublishesUserRegisteredEvent()
    {
        using var db = CreateDb();
        var publishMock = new Mock<IPublishEndpoint>();
        publishMock.Setup(p => p.Publish(It.IsAny<UserRegistered>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(db, publishMock: publishMock);
        await service.ProvisionAsync(ValidRequest());

        publishMock.Verify(p => p.Publish(
            It.Is<UserRegistered>(e => e.Email == "admin@acme.com"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
