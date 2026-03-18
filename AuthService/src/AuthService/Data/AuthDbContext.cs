using Microsoft.EntityFrameworkCore;
using AuthService.Models;

namespace AuthService.Data;

public class AuthDbContext : DbContext
{

  public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
  {
    Users = Set<User>();
    RefreshTokens = Set<RefreshToken>();
    InviteTokens = Set<InviteToken>();
    PasswordResetTokens = Set<PasswordResetToken>();
    Tenants = Set<Tenant>();
  }

  public DbSet<User> Users { get; set; }
  public DbSet<RefreshToken> RefreshTokens { get; set; }
  public DbSet<InviteToken> InviteTokens { get; set; }
  public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
  public DbSet<Tenant> Tenants { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<RefreshToken>()
        .HasIndex(r => r.Token)
        .IsUnique();

    modelBuilder.Entity<InviteToken>()
        .HasIndex(i => i.Token)
        .IsUnique();

    modelBuilder.Entity<PasswordResetToken>()
        .HasIndex(p => p.Token)
        .IsUnique();

    modelBuilder.Entity<Tenant>()
        .HasIndex(t => t.Slug)
        .IsUnique();

    modelBuilder.Entity<User>()
        .HasIndex(u => new { u.TenantId, u.Username })
        .IsUnique();

    modelBuilder.Entity<User>()
        .HasOne(u => u.Tenant)
        .WithMany()
        .HasForeignKey(u => u.TenantId)
        .OnDelete(DeleteBehavior.Restrict);
  }
}
