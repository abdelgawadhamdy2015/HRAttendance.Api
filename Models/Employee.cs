namespace HRAttendance.Api.Models;

public class Employee
{
    public int Id { get; set; }

    // --- Existing HR-app fields (kept for backward compatibility) ---
    public string Code { get; set; } = string.Empty;      // e.g. "001"
    public string FullName { get; set; } = string.Empty;  // legacy single-language name
    public string JobTitle { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }

    // --- Case-statistics domain fields (spec section 3) ---
    public string? NameArabic { get; set; }
    public string? NameEnglish { get; set; }
    public string? GradeArabic { get; set; }
    public string? GradeEnglish { get; set; }
    public DateOnly? JoiningDate { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<Mission> Missions { get; set; } = new List<Mission>();
    public ICollection<PermissionRequest> Permissions { get; set; } = new List<PermissionRequest>();
    public ICollection<EmployeeReport> EmployeeReports { get; set; } = new List<EmployeeReport>();

    /// <summary>Display name: prefers Arabic case-stats name, falls back to legacy FullName.</summary>
    public string DisplayNameArabic => !string.IsNullOrWhiteSpace(NameArabic) ? NameArabic! : FullName;
}

public enum EmployeeStatus
{
    Active = 0,
    OnLeave = 1,
    Seconded = 2,   // معار
    Suspended = 3,
    Retired = 4
}
