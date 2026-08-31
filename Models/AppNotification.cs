namespace HRAttendance.Api.Models;

public class AppNotification
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public NotificationSeverity Severity { get; set; }
    public DateTime CreatedAt { get; set; }
}
