namespace HRAttendance.Api.Models;

public class Employee
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;      // e.g. "001"
    public string FullName { get; set; } = string.Empty;  // أحمد محمد أحمد
    public string JobTitle { get; set; } = string.Empty;   // موظف إداري - الدرجة الثالثة
    public string Department { get; set; } = string.Empty; // إدارة الشؤون الإدارية
    public string? AvatarUrl { get; set; }

    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<Mission> Missions { get; set; } = new List<Mission>();
    public ICollection<PermissionRequest> Permissions { get; set; } = new List<PermissionRequest>();
}
