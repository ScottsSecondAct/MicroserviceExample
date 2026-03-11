using AuthService.Models.DTOs;
using AuthService.Repository;

namespace AuthService.Services;

public class LoginService : ILoginService
{
  private readonly IUserRepository _userRepository;
  private readonly IPasswordService _passwordService;
  private readonly IJwtTokenService _jwtTokenService;
  private readonly IUserRoleClient _userRoleClient;
  private readonly ILogger<LoginService> _logger;

  public LoginService(
      IUserRepository userRepository,
      IPasswordService passwordService,
      IJwtTokenService jwtTokenService,
      IUserRoleClient userRoleClient,
      ILogger<LoginService> logger)
  {
    _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
    _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
    _userRoleClient = userRoleClient ?? throw new ArgumentNullException(nameof(userRoleClient));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task<ServiceResult> LoginAsync(LoginRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
      _logger.LogWarning("Login failed: Missing email or password.");
      return ServiceResult.Failure("Email and password are required.", 400);
    }

    var user = await _userRepository.GetUserByEmailAsync(request.Email);
    if (user == null)
    {
      _logger.LogWarning("Login failed: User with email {Email} not found.", request.Email);
      return ServiceResult.Failure("Invalid email or password.", 401);
    }

    if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
    {
      _logger.LogWarning("Login failed: Invalid password for user with email {Email}.", request.Email);
      return ServiceResult.Failure("Invalid email or password.", 401);
    }

    var role = await _userRoleClient.GetRoleAsync(user.UserId);
    var token = _jwtTokenService.GenerateJwtToken(user, role);

    _logger.LogInformation("User with email {Email} logged in successfully.", request.Email);
    return ServiceResult.Success(token, "Login successful.");
  }
}
