using MassTransit;
using SharedLibrary.DTOs;
using SharedLibrary.Messaging.Events;
using UserManagementService.Services;

namespace UserManagementService.Consumers;

public class UserRegisteredConsumer : IConsumer<UserRegistered>
{
  private readonly IUserProfileService _userProfileService;
  private readonly ILogger<UserRegisteredConsumer> _logger;

  public UserRegisteredConsumer(IUserProfileService userProfileService, ILogger<UserRegisteredConsumer> logger)
  {
    _userProfileService = userProfileService;
    _logger = logger;
  }

  public async Task Consume(ConsumeContext<UserRegistered> context)
  {
    var message = context.Message;
    _logger.LogInformation("Received UserRegistered event for {UserId}", message.UserId);

    var request = new CreateUserProfileRequest
    {
      UserId = message.UserId,
      Email = message.Email
    };

    var result = await _userProfileService.CreateUserProfileAsync(request);

    if (!result.IsSuccess)
    {
      _logger.LogWarning("Failed to create profile for {UserId}: {Message}", message.UserId, result.Message);
    }
    else
    {
      _logger.LogInformation("Profile created for {UserId}", message.UserId);
    }
  }
}
