using AuthService.Models;
using AuthService.Repository;
using Microsoft.Extensions.Configuration;
using SharedLibrary.DTOs;

namespace AuthService.Services;
public class RegistationService : IRegistrationService
{
  private readonly IUserRepository _userRepository;
  private readonly IPasswordService _passwordService;
  private readonly IHttpClientFactory _httpClientFactory;
  private readonly string _userManagementServiceUrl;

  public RegistationService(
      IUserRepository userRepository,
      IPasswordService passwordService,
      IHttpClientFactory httpClientFactory,
      IConfiguration configuration)
  {
    _userRepository = userRepository;
    _passwordService = passwordService;
    _httpClientFactory = httpClientFactory;
    _userManagementServiceUrl = configuration["ServiceUrls:UserManagementService"]
        ?? throw new ArgumentNullException("ServiceUrls:UserManagementService");
  }

  public async Task<bool> ValidateEmailAsync(string email)
  {
    var user = await _userRepository.GetUserByEmailAsync(email);
    return user == null;
  }

  // <summary>
  // Registers a new user.
  // </summary>
  // <param name="email">The email address of the user.</param>
  // <param name="password">The password of the user.</param>
  // <returns>A ServiceResult object containing the result of the registration.</returns>
  public async Task<ServiceResult> RegisterUserAsync(string email, string password)
  {
    if (!await ValidateEmailAsync(email))
    {
      return ServiceResult.Failure("Email is already registered.");
    }

    var user = new User
    {
      UserId = Guid.NewGuid(),
      Email = email,
      PasswordHash = _passwordService.HashPassword(password)
    };

    // Notify the User Management Service that a new user has registered.
    var createUserProfileRequest = new CreateUserProfileRequest
    {
      UserId = user.UserId,
      Email = user.Email
    };

    var client = _httpClientFactory.CreateClient();

    var response = await client.PostAsJsonAsync($"{_userManagementServiceUrl}/api/users", createUserProfileRequest);

    if (response == null || !response.IsSuccessStatusCode)
    {
      return ServiceResult.Failure("Failed to create user profile.");
    }

    var userProfileResponse = await response.Content.ReadFromJsonAsync<CreateUserProfileResponse>();
    if (userProfileResponse == null)
    {
      return ServiceResult.Failure("Failed to parse user profile response.");
    }

    user.Role = userProfileResponse.Role;

    await _userRepository.AddUserAsync(user);

    return ServiceResult.Success(user.UserId, "User registered successfully.");
  }
}
