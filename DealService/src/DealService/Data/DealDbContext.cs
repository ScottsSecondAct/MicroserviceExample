using DealService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Security.Claims;
using System.Text.Json;

namespace DealService.Data;

public class DealDbContext : DbContext
{
  private readonly IHttpContextAccessor? _httpContextAccessor;

  public DealDbContext(DbContextOptions<DealDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
    : base(options)
  {
    _httpContextAccessor = httpContextAccessor;
  }

  public DbSet<Deal> Deals => Set<Deal>();
  public DbSet<DealContact> DealContacts => Set<DealContact>();
  public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<DealContact>()
      .HasOne(dc => dc.Deal)
      .WithMany(d => d.DealContacts)
      .HasForeignKey(dc => dc.DealId)
      .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<Deal>().HasQueryFilter(d => !d.IsDeleted);
  }

  public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    var auditEntries = BuildAuditEntries();
    var result = await base.SaveChangesAsync(cancellationToken);
    if (auditEntries.Count > 0)
    {
      AuditLogs.AddRange(auditEntries);
      await base.SaveChangesAsync(cancellationToken);
    }
    return result;
  }

  private List<AuditLog> BuildAuditEntries()
  {
    ChangeTracker.DetectChanges();
    var userId = GetCurrentUserId();
    var entries = new List<AuditLog>();

    foreach (var entry in ChangeTracker.Entries())
    {
      if (entry.Entity is AuditLog || entry.State == EntityState.Unchanged || entry.State == EntityState.Detached)
        continue;

      var auditLog = new AuditLog
      {
        AuditLogId = Guid.NewGuid(),
        EntityType = entry.Entity.GetType().Name,
        EntityId = GetEntityId(entry),
        ChangedBy = userId,
        ChangedAt = DateTime.UtcNow
      };

      if (entry.State == EntityState.Added)
      {
        auditLog.Action = "Created";
        auditLog.NewValues = SerializeProperties(entry.CurrentValues);
      }
      else if (entry.State == EntityState.Modified)
      {
        auditLog.Action = "Updated";
        auditLog.OldValues = SerializeProperties(entry.OriginalValues);
        auditLog.NewValues = SerializeProperties(entry.CurrentValues);
      }
      else if (entry.State == EntityState.Deleted)
      {
        auditLog.Action = "Deleted";
        auditLog.OldValues = SerializeProperties(entry.OriginalValues);
      }

      entries.Add(auditLog);
    }

    return entries;
  }

  private Guid? GetCurrentUserId()
  {
    var claim = _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)
                ?? _httpContextAccessor?.HttpContext?.User?.FindFirst("UserId");
    if (claim != null && Guid.TryParse(claim.Value, out var userId))
      return userId;
    return null;
  }

  private static string GetEntityId(EntityEntry entry)
  {
    var keyValues = entry.Metadata.FindPrimaryKey()
      ?.Properties
      .Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? string.Empty)
      .ToArray();
    return keyValues != null ? string.Join(",", keyValues) : string.Empty;
  }

  private static string SerializeProperties(PropertyValues values)
  {
    var dict = new Dictionary<string, object?>();
    foreach (var prop in values.Properties)
      dict[prop.Name] = values[prop];
    return JsonSerializer.Serialize(dict);
  }
}
