namespace HRAttendance.Api.Dtos;

public record CreateReportRequest(
    int Year,
    int Month,
    string? OfficeNameArabic,
    string? OfficeNameEnglish);

public record UpdateReportRequest(
    string? OfficeNameArabic,
    string? OfficeNameEnglish);

public record AddEmployeeToReportRequest(int EmployeeId, string? LeaveReason, string? Notes);

public record ReportSummaryDto(
    int Id,
    int Year,
    int Month,
    string OfficeNameArabic,
    string OfficeNameEnglish,
    bool IsFinalized,
    DateTime? FinalizedAt,
    int EmployeeCount,
    int ErrorCount,
    DateTime CreatedAt);

public record PagedResult<T>(List<T> Items, int PageNumber, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

// ---------- Dashboard ----------

public record DashboardKpisDto(
    int TotalCases,
    int CompletedCases,
    int RemainingCases,
    int CurrentYearCases,
    int PreviousYearCases,
    double AchievementPercentage,
    string AchievementPercentageFormatted,
    int ReceivedDuringMonth,
    int ReceivedFromOtherProsecutions);

public record EmployeeAchievementDto(int EmployeeId, string EmployeeName, double AchievementPercentage, int TotalCases);

public record CaseDashboardResponse(
    int ReportId,
    int Year,
    int Month,
    DashboardKpisDto Kpis,
    List<EmployeeAchievementDto> AchievementPerEmployee,
    List<EmployeeAchievementDto> CasesPerEmployee);
