using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Enums;
using SharedLibrary.Messaging.Events;
using UserManagementService.Data;
using UserManagementService.Models;
using UserManagementService.Repository;

namespace UserManagementService.Consumers;

public class UserInvitedConsumer : IConsumer<UserInvited>
{
  private readonly IUserProfileRepository _repository;
  private readonly UserManagementDbContext _db;
  private readonly ILogger<UserInvitedConsumer> _logger;

  public UserInvitedConsumer(IUserProfileRepository repository, UserManagementDbContext db, ILogger<UserInvitedConsumer> logger)
  {
    _repository = repository;
    _db = db;
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

    var tenantId = message.TenantId ?? await GetDefaultTenantIdAsync();

    var profile = new UserProfile
    {
      UserId = message.InvitedUserId,
      Email = message.Email,
      TenantId = tenantId,
      Role = UserRole.Unassigned,
      DisplayName = message.Email,
      IsActive = false,
      InvitePendingAt = message.OccurredAt,
      CreatedAt = message.OccurredAt
    };

    await _repository.AddAsync(profile);
    _logger.LogInformation("Stub profile created for invited user {UserId}", message.InvitedUserId);
  }

  private async Task<Guid> GetDefaultTenantIdAsync()
  {
    var tenant = await _db.Tenants.OrderBy(t => t.CreatedAt).FirstOrDefaultAsync();
    return tenant?.TenantId ?? Guid.Empty;
  }
}
