using Moq;
using MassTransit;
using Microsoft.Extensions.Configuration;
using AuthService.Models;
using AuthService.Repository;
using AuthService.Services;
using SharedLibrary.Messaging.Events;

public class InviteServiceTests
{
  private readonly Mock<IInviteTokenRepository> _mockInviteTokenRepository;
  private readonly Mock<IUserRepository> _mockUserRepository;
  private readonly Mock<IPasswordService> _mockPasswordService;
  private readonly Mock<IEmailService> _mockEmailService;
  private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
  private readonly IConfiguration _configuration;
  private readonly InviteService _service;

  public InviteServiceTests()
  {
    _mockInviteTokenRepository = new Mock<IInviteTokenRepository>();
    _mockUserRepository = new Mock<IUserRepository>();
    _mockPasswordService = new Mock<IPasswordService>();
    _mockEmailService = new Mock<IEmailService>();
    _mockPublishEndpoint = new Mock<IPublishEndpoint>();

    _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["InviteSettings:TokenExpiryHours"] = "48",
          ["InviteSettings:FrontendUrl"] = "http://localhost:3000"
        })
        .Build();

    _service = new InviteService(
        _mockInviteTokenRepository.Object,
        _mockUserRepository.Object,
        _mockPasswordService.Object,
        _mockEmailService.Object,
        _mockPublishEndpoint.Object,
        _configuration);
  }

  [Fact]
  public async Task CreateInviteAsync_ShouldSaveTokenAndSendEmail_WhenEmailIsNew()
  {
    // Arrange
    var email = "newuser@example.com";
    var adminId = Guid.NewGuid();

    _mockUserRepository.Setup(r => r.GetUserByEmailAsync(email)).ReturnsAsync((User?)null);
    _mockInviteTokenRepository.Setup(r => r.AddAsync(It.IsAny<InviteToken>())).Returns(Task.CompletedTask);
    _mockEmailService.Setup(e => e.SendInviteEmailAsync(email, It.IsAny<string>())).Returns(Task.CompletedTask);

    // Act
    var result = await _service.CreateInviteAsync(email, adminId);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(200, result.StatusCode);
    _mockInviteTokenRepository.Verify(
        r => r.AddAsync(It.Is<InviteToken>(t => t.Email == email && !t.IsUsed && t.CreatedByUserId == adminId)),
        Times.Once);
    _mockEmailService.Verify(e => e.SendInviteEmailAsync(email, It.IsAny<string>()), Times.Once);
  }

  [Fact]
  public async Task CreateInviteAsync_ShouldReturnFailure_WhenEmailAlreadyRegistered()
  {
    // Arrange
    var email = "existing@example.com";
    var existing = new User { UserId = Guid.NewGuid(), Email = email, PasswordHash = "hash" };
    _mockUserRepository.Setup(r => r.GetUserByEmailAsync(email)).ReturnsAsync(existing);

    // Act
    var result = await _service.CreateInviteAsync(email, Guid.NewGuid());

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal(409, result.StatusCode);
    _mockInviteTokenRepository.Verify(r => r.AddAsync(It.IsAny<InviteToken>()), Times.Never);
    _mockEmailService.Verify(e => e.SendInviteEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
  }

  [Fact]
  public async Task AcceptInviteAsync_ShouldCreateUserAndPublishEvent_WhenTokenIsValid()
  {
    // Arrange
    var token = "valid-token";
    var password = "SecurePass123";
    var inviteToken = new InviteToken
    {
      Id = Guid.NewGuid(),
      Token = token,
      Email = "invited@example.com",
      ExpiresAt = DateTime.UtcNow.AddHours(24),
      IsUsed = false,
      CreatedByUserId = Guid.NewGuid()
    };

    _mockInviteTokenRepository.Setup(r => r.GetByTokenAsync(token)).ReturnsAsync(inviteToken);
    _mockUserRepository.Setup(r => r.GetUserByEmailAsync(inviteToken.Email)).ReturnsAsync((User?)null);
    _mockPasswordService.Setup(p => p.HashPassword(password)).Returns("hashed-password");
    _mockUserRepository.Setup(r => r.AddUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
    _mockInviteTokenRepository.Setup(r => r.UpdateAsync(It.IsAny<InviteToken>())).Returns(Task.CompletedTask);
    _mockPublishEndpoint
        .Setup(p => p.Publish(It.IsAny<UserRegistered>(), It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

    // Act
    var result = await _service.AcceptInviteAsync(token, password);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(200, result.StatusCode);
    _mockUserRepository.Verify(
        r => r.AddUserAsync(It.Is<User>(u => u.Email == inviteToken.Email)),
        Times.Once);
    _mockInviteTokenRepository.Verify(
        r => r.UpdateAsync(It.Is<InviteToken>(t => t.IsUsed)),
        Times.Once);
    _mockPublishEndpoint.Verify(
        p => p.Publish(It.Is<UserRegistered>(e => e.Email == inviteToken.Email), It.IsAny<CancellationToken>()),
        Times.Once);
  }

  [Fact]
  public async Task AcceptInviteAsync_ShouldReturnFailure_WhenTokenNotFound()
  {
    _mockInviteTokenRepository.Setup(r => r.GetByTokenAsync("bad-token")).ReturnsAsync((InviteToken?)null);

    var result = await _service.AcceptInviteAsync("bad-token", "Password123");

    Assert.False(result.IsSuccess);
    Assert.Equal(400, result.StatusCode);
    _mockUserRepository.Verify(r => r.AddUserAsync(It.IsAny<User>()), Times.Never);
  }

  [Fact]
  public async Task AcceptInviteAsync_ShouldReturnFailure_WhenTokenAlreadyUsed()
  {
    var inviteToken = new InviteToken
    {
      Token = "used-token",
      Email = "user@example.com",
      ExpiresAt = DateTime.UtcNow.AddHours(24),
      IsUsed = true,
      CreatedByUserId = Guid.NewGuid()
    };
    _mockInviteTokenRepository.Setup(r => r.GetByTokenAsync("used-token")).ReturnsAsync(inviteToken);

    var result = await _service.AcceptInviteAsync("used-token", "Password123");

    Assert.False(result.IsSuccess);
    Assert.Equal(400, result.StatusCode);
    Assert.Contains("already been used", result.Message);
  }

  [Fact]
  public async Task AcceptInviteAsync_ShouldReturnFailure_WhenTokenExpired()
  {
    var inviteToken = new InviteToken
    {
      Token = "expired-token",
      Email = "user@example.com",
      ExpiresAt = DateTime.UtcNow.AddHours(-1),
      IsUsed = false,
      CreatedByUserId = Guid.NewGuid()
    };
    _mockInviteTokenRepository.Setup(r => r.GetByTokenAsync("expired-token")).ReturnsAsync(inviteToken);

    var result = await _service.AcceptInviteAsync("expired-token", "Password123");

    Assert.False(result.IsSuccess);
    Assert.Equal(400, result.StatusCode);
    Assert.Contains("expired", result.Message);
  }

  [Fact]
  public async Task AcceptInviteAsync_ShouldReturnFailure_WhenEmailAlreadyRegistered()
  {
    var inviteToken = new InviteToken
    {
      Token = "valid-token",
      Email = "existing@example.com",
      ExpiresAt = DateTime.UtcNow.AddHours(24),
      IsUsed = false,
      CreatedByUserId = Guid.NewGuid()
    };
    var existingUser = new User { UserId = Guid.NewGuid(), Email = "existing@example.com", PasswordHash = "hash" };

    _mockInviteTokenRepository.Setup(r => r.GetByTokenAsync("valid-token")).ReturnsAsync(inviteToken);
    _mockUserRepository.Setup(r => r.GetUserByEmailAsync("existing@example.com")).ReturnsAsync(existingUser);

    var result = await _service.AcceptInviteAsync("valid-token", "Password123");

    Assert.False(result.IsSuccess);
    Assert.Equal(409, result.StatusCode);
  }
}
