using TextilePro.Core.Models;

namespace TextilePro.Core.Services;

public interface IAuditService
{
    void Log(string username, string role, string action, string? tableAffected = null);
    Task<List<AuditLog>> GetLogsAsync(string? username = null, DateTime? from = null, DateTime? to = null);
}
