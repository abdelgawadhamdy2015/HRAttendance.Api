namespace HRAttendance.Api.Models;

public class AuditLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;     // e.g. "Report.Finalize"
    public string Entity { get; set; } = string.Empty;     // e.g. "Report"
    public int? EntityId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? OldValue { get; set; }                  // JSON snapshot
    public string? NewValue { get; set; }                  // JSON snapshot
}
