namespace HRAttendance.Api.Models;

/// <summary>
/// One monthly statistical report for the office (e.g. October 2025).
/// Never hardcode the period anywhere else - it always comes from here.
/// </summary>
public class Report
{
    public int Id { get; set; }

    public int Year { get; set; }
    public int Month { get; set; } // 1-12

    public string OfficeNameArabic { get; set; } = "نيابة الأزهر الإدارية";
    public string OfficeNameEnglish { get; set; } = "Al-Azhar Administrative Prosecution";

    public bool IsFinalized { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public int? FinalizedByUserId { get; set; }
    public User? FinalizedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<EmployeeReport> EmployeeReports { get; set; } = new List<EmployeeReport>();
}
