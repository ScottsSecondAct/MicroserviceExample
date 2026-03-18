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
  private readonly IPasswordPolicyService _passwordPolicyService;
  private readonly IUsernameService _usernameService;
  private readonly ITenantResolver _tenantResolver;
  private readonly IHttpContextAccessor _httpContextAccessor;

  public RegistationService(
      IUserRepository userRepository,
      IPasswordService passwordService,
      IPublishEndpoint publishEndpoint,
      IPasswordPolicyService passwordPolicyService,
      IUsernameService usernameService,
      ITenantResolver tenantResolver,
      IHttpContextAccessor httpContextAccessor)
  {
    _userRepository = userRepository;
    _passwordService = passwordService;
    _publishEndpoint = publishEndpoint;
    _passwordPolicyService = passwordPolicyService;
    _usernameService = usernameService;
    _tenantResolver = tenantResolver;
    _httpContextAccessor = httpContextAccessor;
  }

  public async Task<bool> ValidateEmailAsync(string email)
  {
    var user = await _userRepository.GetUserByEmailAsync(email);
    return user == null;
  }

  public async Task<ServiceResult> RegisterUserAsync(string email, string password)
  {
    var (isValid, errors) = _passwordPolicyService.Validate(password);
    if (!isValid)
      return ServiceResult.Failure(string.Join(" ", errors), 400);

    if (!await ValidateEmailAsync(email))
    {
      return ServiceResult.Failure("Email is already registered.", 409);
    }

    var tenantId = _tenantResolver.Resolve(_httpContextAccessor.HttpContext);
    var username = await _usernameService.DeriveUniqueUsernameAsync(email, tenantId);

    var user = new User
    {
      UserId = Guid.NewGuid(),
      Email = email,
      Username = username,
      TenantId = tenantId,
      PasswordHash = _passwordService.HashPassword(password)
    };

    await _userRepository.AddUserAsync(user);

    await _publishEndpoint.Publish(new UserRegistered
    {
      UserId = user.UserId,
      Email = user.Email,
      Username = user.Username,
      TenantId = user.TenantId
    });

    return ServiceResult.Success(user.UserId, "User registered successfully.");
  }
}
