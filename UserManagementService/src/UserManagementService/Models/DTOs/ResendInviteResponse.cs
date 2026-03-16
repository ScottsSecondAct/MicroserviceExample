namespace UserManagementService.Models.DTOs;

public class ResendInviteResponse
{
  public Guid UserId { get; set; }
  public string Email { get; set; } = string.Empty;
  public DateTime InviteSentAt { get; set; }
}
