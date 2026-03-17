using Microsoft.EntityFrameworkCore;
using UserManagementService.Models;

namespace UserManagementService.Data;

public class UserManagementDbContext : DbContext
{
  public UserManagementDbContext(DbContextOptions<UserManagementDbContext> options) : base(options) { }

  public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
  public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
}
