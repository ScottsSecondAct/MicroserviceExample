using UserManagementService.Models;

namespace UserManagementService.Repository;

public interface IAuditLogRepository
{
  Task AddAsync(AuditLog entry);
  Task<List<AuditLog>> GetAllAsync();
}
