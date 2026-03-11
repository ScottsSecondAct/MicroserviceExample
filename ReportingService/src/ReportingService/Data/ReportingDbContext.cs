using Microsoft.EntityFrameworkCore;
using ReportingService.Models;

namespace ReportingService.Data;

public class ReportingDbContext : DbContext
{
    public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options) { }

    public DbSet<PipelineProjection> PipelineProjections => Set<PipelineProjection>();
    public DbSet<ActivityRepProjection> ActivityRepProjections => Set<ActivityRepProjection>();
    public DbSet<ContactFunnelProjection> ContactFunnelProjections => Set<ContactFunnelProjection>();
    public DbSet<DealSnapshot> DealSnapshots => Set<DealSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PipelineProjection>().HasKey(p => p.Stage);
        modelBuilder.Entity<ActivityRepProjection>().HasKey(a => a.OwnerId);
        modelBuilder.Entity<ContactFunnelProjection>().HasKey(c => c.Status);
        modelBuilder.Entity<DealSnapshot>().HasKey(d => d.DealId);
    }
}
