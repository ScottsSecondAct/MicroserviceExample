using Microsoft.EntityFrameworkCore;
using UserManagementService.Models;

namespace UserManagementService.Data;

public class UserManagementDbContext : DbContext
{
  public UserManagementDbContext(DbContextOptions<UserManagementDbContext> options) : base(options) { }

  public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
  public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
  public DbSet<Tenant> Tenants => Set<Tenant>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<Tenant>()
        .HasIndex(t => t.Slug)
        .IsUnique();

    modelBuilder.Entity<UserProfile>()
        .HasOne(p => p.Tenant)
        .WithMany()
        .HasForeignKey(p => p.TenantId)
        .OnDelete(DeleteBehavior.Restrict);
  }
}
