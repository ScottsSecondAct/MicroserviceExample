using AuthService.Models;
using AuthService.Repository;
using MassTransit;
using SharedLibrary.Messaging.Events;

namespace AuthService.Services;

public class RegistationService : IRegistrationService
{
  private readonly IUserRepository _userRepository;
  private readonly IPasswordService _passwordService;
  private readonly IPublishEndpoint _publishEndpoint;

  public RegistationService(
      IUserRepository userRepository,
      IPasswordService passwordService,
      IPublishEndpoint publishEndpoint)
  {
    _userRepository = userRepository;
    _passwordService = passwordService;
    _publishEndpoint = publishEndpoint;
  }

  public async Task<bool> ValidateEmailAsync(string email)
  {
    var user = await _userRepository.GetUserByEmailAsync(email);
    return user == null;
  }

  public async Task<ServiceResult> RegisterUserAsync(string email, string password)
  {
    if (!await ValidateEmailAsync(email))
    {
      return ServiceResult.Failure("Email is already registered.", 409);
    }

    var user = new User
    {
      UserId = Guid.NewGuid(),
      Email = email,
      PasswordHash = _passwordService.HashPassword(password)
    };

    await _userRepository.AddUserAsync(user);

    await _publishEndpoint.Publish(new UserRegistered
    {
      UserId = user.UserId,
      Email = user.Email
    });

    return ServiceResult.Success(user.UserId, "User registered successfully.");
  }
}
