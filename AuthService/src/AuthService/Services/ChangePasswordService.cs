using AuthService.Models.DTOs;
using AuthService.Repository;

namespace AuthService.Services;

public class ChangePasswordService : IChangePasswordService
{
  private readonly IUserRepository _userRepository;
  private readonly IPasswordService _passwordService;
  private readonly IJwtTokenService _jwtTokenService;
  private readonly IUserRoleClient _userRoleClient;
  private readonly ILogger<ChangePasswordService> _logger;
  private readonly IPasswordPolicyService _passwordPolicyService;

  public ChangePasswordService(
      IUserRepository userRepository,
      IPasswordService passwordService,
      IJwtTokenService jwtTokenService,
      IUserRoleClient userRoleClient,
      ILogger<ChangePasswordService> logger,
      IPasswordPolicyService passwordPolicyService)
  {
    _userRepository = userRepository;
    _passwordService = passwordService;
    _jwtTokenService = jwtTokenService;
    _userRoleClient = userRoleClient;
    _logger = logger;
    _passwordPolicyService = passwordPolicyService;
  }

  public async Task<ServiceResult> ChangePasswordAsync(Guid userId, string newPassword)
  {
    if (string.IsNullOrWhiteSpace(newPassword))
    {
      return ServiceResult.Failure("New password is required.", 400);
    }

    var (isValid, errors) = _passwordPolicyService.Validate(newPassword);
    if (!isValid)
      return ServiceResult.Failure(string.Join(" ", errors), 400);

    var user = await _userRepository.GetUserByIdAsync(userId);
    if (user == null)
    {
      _logger.LogWarning("ChangePassword failed: User {UserId} not found.", userId);
      return ServiceResult.Failure("User not found.", 404);
    }

    user.PasswordHash = _passwordService.HashPassword(newPassword);
    user.MustChangePassword = false;
    await _userRepository.UpdateUserAsync(user);

    var userStatus = await _userRoleClient.GetRoleAsync(userId);
    var newToken = _jwtTokenService.GenerateJwtToken(user, userStatus.Role);

    _logger.LogInformation("User {UserId} changed password successfully.", userId);
    return ServiceResult.Success(new LoginResponse { Token = newToken }, "Password changed successfully.");
  }
}
