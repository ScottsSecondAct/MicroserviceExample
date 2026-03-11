using SharedLibrary.DTOs;
using SharedLibrary.Enums;
using UserManagementService.Models;
using UserManagementService.Models.DTOs;
using UserManagementService.Repository;
using Microsoft.EntityFrameworkCore;

namespace UserManagementService.Services;

public class UserProfileService : IUserProfileService
{
  private readonly IUserProfileRepository _repository;

  public UserProfileService(IUserProfileRepository repository)
  {
    _repository = repository;
  }

  public async Task<ServiceResult> CreateUserProfileAsync(CreateUserProfileRequest request)
  {
    var existing = await _repository.GetByIdAsync(request.UserId);
    if (existing != null)
      return ServiceResult.Failure("User profile already exists.");

    var profile = new UserProfile
    {
      UserId = request.UserId,
      Email = request.Email,
      Role = UserRole.Member,
      DisplayName = request.Email,
      CreatedAt = DateTime.UtcNow
    };

    await _repository.AddAsync(profile);

    return ServiceResult.Success(new CreateUserProfileResponse
    {
      UserId = profile.UserId,
      Role = profile.Role
    }, "User profile created successfully.", 201);
  }

  public async Task<ServiceResult> GetUserProfileAsync(Guid userId)
  {
    var profile = await _repository.GetByIdAsync(userId);
    if (profile == null)
      return ServiceResult.Failure("User profile not found.", 404);

    return ServiceResult.Success(profile);
  }

  public async Task<ServiceResult> GetUserRoleAsync(Guid userId)
  {
    var profile = await _repository.GetByIdAsync(userId);
    if (profile == null)
      return ServiceResult.Failure("User profile not found.", 404);

    return ServiceResult.Success(new UserRoleResponse
    {
      UserId = profile.UserId,
      Role = profile.Role
    });
  }

  public async Task<ServiceResult> GetTeamAsync()
  {
    var profiles = await _repository.GetAllAsync();
    var team = profiles.Select(p => new TeamMemberResponse
    {
      UserId = p.UserId,
      DisplayName = p.DisplayName,
      Role = p.Role
    }).ToList();

    return ServiceResult.Success(team);
  }
}
