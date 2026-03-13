using Microsoft.EntityFrameworkCore;
using AuthService.Models;

namespace AuthService.Data;

public class AuthDbContext : DbContext
{

  public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
  {
    Users = Set<User>();
    RefreshTokens = Set<RefreshToken>();
  }

  public DbSet<User> Users { get; set; }
  public DbSet<RefreshToken> RefreshTokens { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<RefreshToken>()
        .HasIndex(r => r.Token)
        .IsUnique();
  }
}
