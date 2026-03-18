using System.Security.Cryptography;
using AuthService.Models;
using AuthService.Models.DTOs;
using AuthService.Repository;

namespace AuthService.Services;

public class LoginService : ILoginService
{
  private static readonly TimeSpan RefreshTokenExpiry = TimeSpan.FromDays(7);

  private readonly IUserRepository _userRepository;
  private readonly IPasswordService _passwordService;
  private readonly IJwtTokenService _jwtTokenService;
  private readonly IUserRoleClient _userRoleClient;
  private readonly IRefreshTokenRepository _refreshTokenRepository;
  private readonly ILogger<LoginService> _logger;
  private readonly ITenantResolver _tenantResolver;
  private readonly IHttpContextAccessor _httpContextAccessor;

  public LoginService(
      IUserRepository userRepository,
      IPasswordService passwordService,
      IJwtTokenService jwtTokenService,
      IUserRoleClient userRoleClient,
      IRefreshTokenRepository refreshTokenRepository,
      ILogger<LoginService> logger,
      ITenantResolver tenantResolver,
      IHttpContextAccessor httpContextAccessor)
  {
    _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
    _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
    _userRoleClient = userRoleClient ?? throw new ArgumentNullException(nameof(userRoleClient));
    _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _tenantResolver = tenantResolver ?? throw new ArgumentNullException(nameof(tenantResolver));
    _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
  }

  public async Task<ServiceResult> LoginAsync(LoginRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
      _logger.LogWarning("Login failed: Missing email or password.");
      return ServiceResult.Failure("Email and password are required.", 400);
    }

    User? user;
    if (request.Email.Contains('@'))
    {
      user = await _userRepository.GetUserByEmailAsync(request.Email);
    }
    else
    {
      var tenantId = _tenantResolver.Resolve(_httpContextAccessor.HttpContext);
      user = await _userRepository.GetUserByUsernameAsync(tenantId, request.Email);
    }

    if (user == null)
    {
      _logger.LogWarning("Login failed: User {Identifier} not found.", request.Email);
      return ServiceResult.Failure("Invalid email or password.", 401);
    }

    if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
    {
      _logger.LogWarning("Login failed: Invalid password for user {UserId}.", user.UserId);
      return ServiceResult.Failure("Invalid email or password.", 401);
    }

    var userStatus = await _userRoleClient.GetRoleAsync(user.UserId);
    if (!userStatus.IsActive)
    {
      _logger.LogWarning("Login failed: User {UserId} is deactivated.", user.UserId);
      return ServiceResult.Failure("Account is deactivated. Please contact an administrator.", 403);
    }

    var jwtToken = _jwtTokenService.GenerateJwtToken(user, userStatus.Role);
    var refreshToken = await CreateRefreshTokenAsync(user.UserId);

    _logger.LogInformation("User with email {Email} logged in successfully.", user.Email);
    return ServiceResult.Success(new LoginResponse { Token = jwtToken, RefreshToken = refreshToken }, "Login successful.");
  }

  public async Task<ServiceResult> RefreshAsync(RefreshRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.RefreshToken))
    {
      return ServiceResult.Failure("Refresh token is required.", 400);
    }

    var existing = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);

    if (existing == null || existing.IsRevoked || existing.ExpiresAt < DateTime.UtcNow)
    {
      _logger.LogWarning("Refresh failed: token invalid, revoked, or expired.");
      return ServiceResult.Failure("Invalid or expired refresh token.", 401);
    }

    await _refreshTokenRepository.RevokeAsync(existing);

    var userStatus = await _userRoleClient.GetRoleAsync(existing.UserId);
    if (!userStatus.IsActive)
    {
      _logger.LogWarning("Refresh failed: User {UserId} is deactivated.", existing.UserId);
      return ServiceResult.Failure("Account is deactivated.", 403);
    }

    var newJwt = _jwtTokenService.GenerateJwtToken(existing.User, userStatus.Role);
    var newRefreshToken = await CreateRefreshTokenAsync(existing.UserId);

    _logger.LogInformation("Refresh token rotated for user {UserId}.", existing.UserId);
    return ServiceResult.Success(new LoginResponse { Token = newJwt, RefreshToken = newRefreshToken }, "Token refreshed.");
  }

  private async Task<string> CreateRefreshTokenAsync(Guid userId)
  {
    var tokenBytes = RandomNumberGenerator.GetBytes(64);
    var token = Convert.ToBase64String(tokenBytes);

    await _refreshTokenRepository.AddAsync(new RefreshToken
    {
      Id = Guid.NewGuid(),
      Token = token,
      UserId = userId,
      ExpiresAt = DateTime.UtcNow.Add(RefreshTokenExpiry),
      IsRevoked = false,
      CreatedAt = DateTime.UtcNow
    });

    return token;
  }
}
