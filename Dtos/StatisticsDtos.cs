namespace HRAttendance.Api.Dtos;

// ---------- Manual-input request bodies (what the client submits/edits) ----------

public record PreviousYearStatisticsInput(
    int PendingDecision,
    int TotalPreviousYearCases,
    int CompletedCases,
    int UnderInvestigation,
    int TechnicalOfficeSent,
    int TechnicalOfficeUnderCopying,
    int BranchSent,
    int BranchUnderCopying,
    int CentralAdministrations,
    int FollowUpCases,
    int SpatialVariableCases);

public record CurrentYearStatisticsInput(
    int TransferredFromBeginningOfYear,
    int ReceivedDuringMonth,
    int CompletedCases,
    int UnderInvestigation,
    int TechnicalOfficeSent,
    int TechnicalOfficeUnderCopying,
    int BranchSent,
    int BranchUnderCopying,
    int CentralAdministrations,
    int FollowUpCases,
    int SpatialVariableCases,
    int ReceivedFromOtherProsecutions,
    int PendingDecision);

public record UpdateEmployeeStatisticsRequest(
    string? LeaveReason,
    string? Notes,
    PreviousYearStatisticsInput PreviousYear,
    CurrentYearStatisticsInput CurrentYear);

// ---------- Computed (server-derived) response shapes ----------

/// <summary>Previous-year figures + the one derived field (RemainingCases).</summary>
public record PreviousYearStatisticsDto(
    int PendingDecision,
    int TotalPreviousYearCases,
    int CompletedCases,
    int RemainingCases,           // derived: Total - Completed
    int UnderInvestigation,
    int TechnicalOfficeSent,
    int TechnicalOfficeUnderCopying,
    int BranchSent,
    int BranchUnderCopying,
    int CentralAdministrations,
    int FollowUpCases,
    int SpatialVariableCases);

/// <summary>Current-year figures + the two derived fields.</summary>
public record CurrentYearStatisticsDto(
    int TransferredFromBeginningOfYear,
    int ReceivedDuringMonth,
    int TotalRegisteredCases,     // derived: Transferred + Received
    int CompletedCases,
    int RemainingCases,           // derived: TotalRegistered - Completed
    int UnderInvestigation,
    int TechnicalOfficeSent,
    int TechnicalOfficeUnderCopying,
    int BranchSent,
    int BranchUnderCopying,
    int CentralAdministrations,
    int FollowUpCases,
    int SpatialVariableCases,
    int ReceivedFromOtherProsecutions,
    int PendingDecision);

/// <summary>
/// The full calculated payload for one employee within one report.
/// This is the exact shape requested in spec section 24.
/// </summary>
public record EmployeeReportStatisticsDto(
    int EmployeeReportId,
    int EmployeeId,
    string EmployeeNameArabic,
    string? EmployeeNameEnglish,
    string? GradeArabic,
    DateOnly? JoiningDate,
    string? LeaveReason,
    string? Notes,
    PreviousYearStatisticsDto PreviousYear,
    CurrentYearStatisticsDto CurrentYear,
    int TotalCasesAvailableForProcessing,   // PreviousYear.Total + CurrentYear.TotalRegistered
    int TotalCompletedCases,                 // PreviousYear.Completed + CurrentYear.Completed
    int TotalRemainingCases,                 // PreviousYear.Remaining + CurrentYear.Remaining
    double AchievementPercentage,            // decimal, e.g. 0.8387
    string AchievementPercentageFormatted,   // e.g. "83.87%"
    bool HasError,
    List<string> Errors);

/// <summary>Totals row across every employee in a report - aggregated, not averaged (spec section 25).</summary>
public record ReportTotalsDto(
    int TotalPreviousYearCases,
    int TotalPreviousYearCompleted,
    int TotalPreviousYearRemaining,
    int TotalCurrentYearReceived,
    int TotalCurrentYearRegistered,
    int TotalCurrentYearCompleted,
    int TotalCurrentYearRemaining,
    int TotalPendingDecision,
    int TotalTechnicalOffice,
    int TotalBranch,
    int TotalCentralAdministrations,
    int TotalFollowUpCases,
    int TotalSpatialVariableCases,
    double OverallAchievementPercentage,
    string OverallAchievementPercentageFormatted);

public record ReportStatisticsResponse(
    int ReportId,
    int Year,
    int Month,
    bool IsFinalized,
    List<EmployeeReportStatisticsDto> Employees,
    ReportTotalsDto Totals);
