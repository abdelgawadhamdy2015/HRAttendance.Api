using System.Text.Json;
using HRAttendance.Api.Data;
using HRAttendance.Api.Models;

namespace HRAttendance.Api.Services;

public interface IAuditService
{
    Task LogAsync(int? userId, string action, string entity, int? entityId, object? oldValue = null, object? newValue = null);
}

public sealed class AuditService(AppDbContext db) : IAuditService
{
    private readonly AppDbContext _db = db;

    public async Task LogAsync(int? userId, string action, string entity, int? entityId, object? oldValue = null, object? newValue = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            OldValue = oldValue is null ? null : JsonSerializer.Serialize(oldValue),
            NewValue = newValue is null ? null : JsonSerializer.Serialize(newValue),
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
