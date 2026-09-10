namespace HRAttendance.Api.Models;

/// <summary>
/// Connects one Employee to one monthly Report. Unique per (ReportId, EmployeeId).
/// </summary>
public class EmployeeReport
{
    public int Id { get; set; }

    public int ReportId { get; set; }
    public Report? Report { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    /// <summary>Free-text special status for the month, e.g. "رعاية طفل". Nullable.</summary>
    public string? LeaveReason { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Persisted for fast list queries only - always recomputed server-side by
    /// StatisticsCalculationService before save. Never trust a client-sent value.
    /// </summary>
    public bool HasError { get; set; }

    public PreviousYearStatistics? PreviousYearStatistics { get; set; }
    public CurrentYearStatistics? CurrentYearStatistics { get; set; }
}

/// <summary>
/// Manually-entered previous-year case figures for one employee/report.
/// RemainingCases is NEVER stored - it is always derived
/// (TotalPreviousYearCases - CompletedCases) by the calculation service.
/// </summary>
public class PreviousYearStatistics
{
    public int Id { get; set; }

    public int EmployeeReportId { get; set; }
    public EmployeeReport? EmployeeReport { get; set; }

    // --- Manual (source) fields ---
    public int PendingDecision { get; set; }
    public int TotalPreviousYearCases { get; set; }
    public int CompletedCases { get; set; }
    public int UnderInvestigation { get; set; }
    public int TechnicalOfficeSent { get; set; }
    public int TechnicalOfficeUnderCopying { get; set; }
    public int BranchSent { get; set; }
    public int BranchUnderCopying { get; set; }
    public int CentralAdministrations { get; set; }
    public int FollowUpCases { get; set; }
    public int SpatialVariableCases { get; set; }
}

/// <summary>
/// Manually-entered current-year case figures for one employee/report.
/// TotalRegisteredCases and RemainingCases are NEVER stored - always derived.
/// </summary>
public class CurrentYearStatistics
{
    public int Id { get; set; }

    public int EmployeeReportId { get; set; }
    public EmployeeReport? EmployeeReport { get; set; }

    // --- Manual (source) fields ---
    public int TransferredFromBeginningOfYear { get; set; }
    public int ReceivedDuringMonth { get; set; }
    public int CompletedCases { get; set; }
    public int UnderInvestigation { get; set; }
    public int TechnicalOfficeSent { get; set; }
    public int TechnicalOfficeUnderCopying { get; set; }
    public int BranchSent { get; set; }
    public int BranchUnderCopying { get; set; }
    public int CentralAdministrations { get; set; }
    public int FollowUpCases { get; set; }
    public int SpatialVariableCases { get; set; }
    public int ReceivedFromOtherProsecutions { get; set; }
    public int PendingDecision { get; set; }
}
