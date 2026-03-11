using DealService.Models;
using Microsoft.EntityFrameworkCore;

namespace DealService.Data;

public class DealDbContext : DbContext
{
  public DealDbContext(DbContextOptions<DealDbContext> options) : base(options) { }

  public DbSet<Deal> Deals => Set<Deal>();
  public DbSet<DealContact> DealContacts => Set<DealContact>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<DealContact>()
      .HasOne(dc => dc.Deal)
      .WithMany(d => d.DealContacts)
      .HasForeignKey(dc => dc.DealId)
      .OnDelete(DeleteBehavior.Cascade);
  }
}
