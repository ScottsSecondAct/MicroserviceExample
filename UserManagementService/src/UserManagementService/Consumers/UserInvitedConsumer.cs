using MassTransit;
using SharedLibrary.Enums;
using SharedLibrary.Messaging.Events;
using UserManagementService.Models;
using UserManagementService.Repository;

namespace UserManagementService.Consumers;

public class UserInvitedConsumer : IConsumer<UserInvited>
{
  private readonly IUserProfileRepository _repository;
  private readonly ILogger<UserInvitedConsumer> _logger;

  public UserInvitedConsumer(IUserProfileRepository repository, ILogger<UserInvitedConsumer> logger)
  {
    _repository = repository;
    _logger = logger;
  }

  public async Task Consume(ConsumeContext<UserInvited> context)
  {
    var message = context.Message;
    _logger.LogInformation("Received UserInvited event for {UserId} ({Email})", message.InvitedUserId, message.Email);

    var existing = await _repository.GetByIdAsync(message.InvitedUserId);
    if (existing != null)
    {
      _logger.LogInformation("Stub profile already exists for {UserId} — skipping", message.InvitedUserId);
      return;
    }

    var profile = new UserProfile
    {
      UserId = message.InvitedUserId,
      Email = message.Email,
      Role = UserRole.Unassigned,
      DisplayName = message.Email,
      IsActive = false,
      InvitePendingAt = message.OccurredAt,
      CreatedAt = message.OccurredAt
    };

    await _repository.AddAsync(profile);
    _logger.LogInformation("Stub profile created for invited user {UserId}", message.InvitedUserId);
  }
}
