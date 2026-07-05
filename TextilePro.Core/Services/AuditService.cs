using Microsoft.EntityFrameworkCore;
using TextilePro.Core.DbContext;
using TextilePro.Core.Models;

namespace TextilePro.Core.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _context;

    public AuditService(AppDbContext context)
    {
        _context = context;
    }

    public void Log(string username, string role, string action, string? tableAffected = null)
    {
        var log = new AuditLog
        {
            Timestamp = DateTime.Now,
            Username = username,
            Role = role,
            Action = action,
            TableAffected = tableAffected
        };
        _context.AuditLogs.Add(log);
        _context.SaveChanges();
    }

    public async Task<List<AuditLog>> GetLogsAsync(string? username = null, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AuditLogs.AsQueryable();
        if (!string.IsNullOrEmpty(username))
            query = query.Where(l => l.Username == username);
        if (from.HasValue)
            query = query.Where(l => l.Timestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(l => l.Timestamp <= to.Value);
        return await query.OrderByDescending(l => l.Timestamp).ToListAsync();
    }
}