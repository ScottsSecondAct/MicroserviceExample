using System.Security.Cryptography;
using AuthService.Models;
using AuthService.Repository;
using MassTransit;
using SharedLibrary.Messaging.Events;

namespace AuthService.Services;

public class InviteService : IInviteService
{
  private readonly IInviteTokenRepository _inviteTokenRepository;
  private readonly IUserRepository _userRepository;
  private readonly IPasswordService _passwordService;
  private readonly IEmailService _emailService;
  private readonly IPublishEndpoint _publishEndpoint;
  private readonly IConfiguration _configuration;
  private readonly IPasswordPolicyService _passwordPolicyService;

  public InviteService(
      IInviteTokenRepository inviteTokenRepository,
      IUserRepository userRepository,
      IPasswordService passwordService,
      IEmailService emailService,
      IPublishEndpoint publishEndpoint,
      IConfiguration configuration,
      IPasswordPolicyService passwordPolicyService)
  {
    _inviteTokenRepository = inviteTokenRepository;
    _userRepository = userRepository;
    _passwordService = passwordService;
    _emailService = emailService;
    _publishEndpoint = publishEndpoint;
    _configuration = configuration;
    _passwordPolicyService = passwordPolicyService;
  }

  public async Task<ServiceResult> CreateInviteAsync(string email, Guid adminUserId)
  {
    var existing = await _userRepository.GetUserByEmailAsync(email);
    if (existing != null)
    {
      return ServiceResult.Failure("A user with this email is already registered.", 409);
    }

    var tokenBytes = RandomNumberGenerator.GetBytes(32);
    var token = Convert.ToBase64String(tokenBytes)
        .Replace('+', '-').Replace('/', '_').Replace("=", string.Empty);

    var expiryHours = _configuration.GetValue<int>("InviteSettings:TokenExpiryHours", 48);

    var inviteToken = new InviteToken
    {
      Id = Guid.NewGuid(),
      Token = token,
      Email = email,
      ExpiresAt = DateTime.UtcNow.AddHours(expiryHours),
      IsUsed = false,
      CreatedByUserId = adminUserId,
      CreatedAt = DateTime.UtcNow
    };

    await _inviteTokenRepository.AddAsync(inviteToken);
    await _emailService.SendInviteEmailAsync(email, token);

    return ServiceResult.Success(null, "Invite sent successfully.");
  }

  public async Task<ServiceResult> AcceptInviteAsync(string token, string password)
  {
    var (isValid, errors) = _passwordPolicyService.Validate(password);
    if (!isValid)
      return ServiceResult.Failure(string.Join(" ", errors), 400);

    var inviteToken = await _inviteTokenRepository.GetByTokenAsync(token);

    if (inviteToken == null)
    {
      return ServiceResult.Failure("Invalid or expired invite token.", 400);
    }

    if (inviteToken.IsUsed)
    {
      return ServiceResult.Failure("This invite has already been used.", 400);
    }

    if (inviteToken.ExpiresAt < DateTime.UtcNow)
    {
      return ServiceResult.Failure("This invite has expired.", 400);
    }

    var existing = await _userRepository.GetUserByEmailAsync(inviteToken.Email);
    if (existing != null)
    {
      return ServiceResult.Failure("A user with this email is already registered.", 409);
    }

    var user = new User
    {
      UserId = Guid.NewGuid(),
      Email = inviteToken.Email,
      PasswordHash = _passwordService.HashPassword(password),
      MustChangePassword = true
    };

    await _userRepository.AddUserAsync(user);

    inviteToken.IsUsed = true;
    await _inviteTokenRepository.UpdateAsync(inviteToken);

    await _publishEndpoint.Publish(new UserRegistered
    {
      UserId = user.UserId,
      Email = user.Email
    });

    return ServiceResult.Success(null, "Account created successfully.");
  }
}
