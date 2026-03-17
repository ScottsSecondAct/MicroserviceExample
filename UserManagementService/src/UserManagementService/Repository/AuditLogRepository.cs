using Microsoft.EntityFrameworkCore;
using UserManagementService.Data;
using UserManagementService.Models;

namespace UserManagementService.Repository;

public class AuditLogRepository : IAuditLogRepository
{
  private readonly UserManagementDbContext _context;

  public AuditLogRepository(UserManagementDbContext context)
  {
    _context = context;
  }

  public async Task AddAsync(AuditLog entry)
  {
    _context.AuditLogs.Add(entry);
    await _context.SaveChangesAsync();
  }

  public async Task<List<AuditLog>> GetAllAsync() =>
    await _context.AuditLogs
      .OrderByDescending(a => a.Timestamp)
      .ToListAsync();
}
