using System.Security.Cryptography;
using AuthService.Models;
using AuthService.Repository;

namespace AuthService.Services;

public class ForgotPasswordService : IForgotPasswordService
{
  private readonly IUserRepository _userRepository;
  private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
  private readonly IPasswordService _passwordService;
  private readonly IEmailService _emailService;

  public ForgotPasswordService(
      IUserRepository userRepository,
      IPasswordResetTokenRepository passwordResetTokenRepository,
      IPasswordService passwordService,
      IEmailService emailService)
  {
    _userRepository = userRepository;
    _passwordResetTokenRepository = passwordResetTokenRepository;
    _passwordService = passwordService;
    _emailService = emailService;
  }

  public async Task<ServiceResult> ForgotPasswordAsync(string email)
  {
    // Always return success to avoid email enumeration
    var user = await _userRepository.GetUserByEmailAsync(email);
    if (user == null)
    {
      return ServiceResult.Success(null, "If that email is registered, a reset link has been sent.");
    }

    var tokenBytes = RandomNumberGenerator.GetBytes(32);
    var token = Convert.ToBase64String(tokenBytes)
        .Replace('+', '-').Replace('/', '_').Replace("=", string.Empty);

    var resetToken = new PasswordResetToken
    {
      Id = Guid.NewGuid(),
      Token = token,
      UserId = user.UserId,
      Email = user.Email,
      ExpiresAt = DateTime.UtcNow.AddHours(1),
      IsUsed = false,
      CreatedAt = DateTime.UtcNow
    };

    await _passwordResetTokenRepository.AddAsync(resetToken);
    await _emailService.SendPasswordResetEmailAsync(email, token);

    return ServiceResult.Success(null, "If that email is registered, a reset link has been sent.");
  }

  public async Task<ServiceResult> ResetPasswordAsync(string token, string newPassword)
  {
    var resetToken = await _passwordResetTokenRepository.GetByTokenAsync(token);

    if (resetToken == null)
    {
      return ServiceResult.Failure("Invalid or expired reset token.", 400);
    }

    if (resetToken.IsUsed)
    {
      return ServiceResult.Failure("This reset link has already been used.", 400);
    }

    if (resetToken.ExpiresAt < DateTime.UtcNow)
    {
      return ServiceResult.Failure("This reset link has expired.", 400);
    }

    var user = await _userRepository.GetUserByIdAsync(resetToken.UserId);
    if (user == null)
    {
      return ServiceResult.Failure("User not found.", 400);
    }

    user.PasswordHash = _passwordService.HashPassword(newPassword);
    await _userRepository.UpdateUserAsync(user);

    resetToken.IsUsed = true;
    await _passwordResetTokenRepository.UpdateAsync(resetToken);

    return ServiceResult.Success(null, "Password has been reset successfully.");
  }
}
