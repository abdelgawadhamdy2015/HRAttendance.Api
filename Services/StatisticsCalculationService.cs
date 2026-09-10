using HRAttendance.Api.Dtos;
using HRAttendance.Api.Models;

namespace HRAttendance.Api.Services;

/// <summary>
/// Every calculated field in the case-statistics domain is produced here and
/// ONLY here. Nothing else in the codebase should re-derive RemainingCases,
/// TotalRegisteredCases, or AchievementPercentage - this keeps the "never
/// trust manually stored calculated values" rule (spec section 24) enforceable
/// in one place.
/// </summary>
public interface IStatisticsCalculationService
{
    EmployeeReportStatisticsDto Calculate(EmployeeReport er);
    ReportTotalsDto CalculateTotals(IEnumerable<EmployeeReportStatisticsDto> employeeStats);
}

public sealed class StatisticsCalculationService : IStatisticsCalculationService
{
    public EmployeeReportStatisticsDto Calculate(EmployeeReport er)
    {
        var prevSrc = er.PreviousYearStatistics
            ?? throw new InvalidOperationException("EmployeeReport is missing PreviousYearStatistics.");
        var currSrc = er.CurrentYearStatistics
            ?? throw new InvalidOperationException("EmployeeReport is missing CurrentYearStatistics.");

        var prevRemaining = prevSrc.TotalPreviousYearCases - prevSrc.CompletedCases;
        var prev = new PreviousYearStatisticsDto(
            PendingDecision: prevSrc.PendingDecision,
            TotalPreviousYearCases: prevSrc.TotalPreviousYearCases,
            CompletedCases: prevSrc.CompletedCases,
            RemainingCases: prevRemaining,
            UnderInvestigation: prevSrc.UnderInvestigation,
            TechnicalOfficeSent: prevSrc.TechnicalOfficeSent,
            TechnicalOfficeUnderCopying: prevSrc.TechnicalOfficeUnderCopying,
            BranchSent: prevSrc.BranchSent,
            BranchUnderCopying: prevSrc.BranchUnderCopying,
            CentralAdministrations: prevSrc.CentralAdministrations,
            FollowUpCases: prevSrc.FollowUpCases,
            SpatialVariableCases: prevSrc.SpatialVariableCases);

        var currTotal = currSrc.TransferredFromBeginningOfYear + currSrc.ReceivedDuringMonth;
        var currRemaining = currTotal - currSrc.CompletedCases;
        var curr = new CurrentYearStatisticsDto(
            TransferredFromBeginningOfYear: currSrc.TransferredFromBeginningOfYear,
            ReceivedDuringMonth: currSrc.ReceivedDuringMonth,
            TotalRegisteredCases: currTotal,
            CompletedCases: currSrc.CompletedCases,
            RemainingCases: currRemaining,
            UnderInvestigation: currSrc.UnderInvestigation,
            TechnicalOfficeSent: currSrc.TechnicalOfficeSent,
            TechnicalOfficeUnderCopying: currSrc.TechnicalOfficeUnderCopying,
            BranchSent: currSrc.BranchSent,
            BranchUnderCopying: currSrc.BranchUnderCopying,
            CentralAdministrations: currSrc.CentralAdministrations,
            FollowUpCases: currSrc.FollowUpCases,
            SpatialVariableCases: currSrc.SpatialVariableCases,
            ReceivedFromOtherProsecutions: currSrc.ReceivedFromOtherProsecutions,
            PendingDecision: currSrc.PendingDecision);

        var totalAvailable = prev.TotalPreviousYearCases + curr.TotalRegisteredCases;
        var totalCompleted = prev.CompletedCases + curr.CompletedCases;
        var totalRemaining = prev.RemainingCases + curr.RemainingCases;

        var achievement = SafeDivide(totalCompleted, totalAvailable);

        var errors = DetectErrors(prev, curr, totalAvailable, totalCompleted, er.Employee);

        var employee = er.Employee;
        return new EmployeeReportStatisticsDto(
            EmployeeReportId: er.Id,
            EmployeeId: er.EmployeeId,
            EmployeeNameArabic: employee?.DisplayNameArabic ?? string.Empty,
            EmployeeNameEnglish: employee?.NameEnglish,
            GradeArabic: employee?.GradeArabic,
            JoiningDate: employee?.JoiningDate,
            LeaveReason: er.LeaveReason,
            Notes: er.Notes,
            PreviousYear: prev,
            CurrentYear: curr,
            TotalCasesAvailableForProcessing: totalAvailable,
            TotalCompletedCases: totalCompleted,
            TotalRemainingCases: totalRemaining,
            AchievementPercentage: achievement,
            AchievementPercentageFormatted: FormatPercentage(achievement),
            HasError: errors.Count > 0,
            Errors: errors);
    }

    public ReportTotalsDto CalculateTotals(IEnumerable<EmployeeReportStatisticsDto> employeeStats)
    {
        var list = employeeStats as IReadOnlyCollection<EmployeeReportStatisticsDto> ?? employeeStats.ToList();

        int prevTotal = 0, prevCompleted = 0, prevRemaining = 0;
        int currReceived = 0, currRegistered = 0, currCompleted = 0, currRemaining = 0;
        int pending = 0, technicalOffice = 0, branch = 0, central = 0, followUp = 0, spatial = 0;

        foreach (var e in list)
        {
            prevTotal += e.PreviousYear.TotalPreviousYearCases;
            prevCompleted += e.PreviousYear.CompletedCases;
            prevRemaining += e.PreviousYear.RemainingCases;

            currReceived += e.CurrentYear.ReceivedDuringMonth;
            currRegistered += e.CurrentYear.TotalRegisteredCases;
            currCompleted += e.CurrentYear.CompletedCases;
            currRemaining += e.CurrentYear.RemainingCases;

            pending += e.PreviousYear.PendingDecision + e.CurrentYear.PendingDecision;
            technicalOffice += e.PreviousYear.TechnicalOfficeSent + e.PreviousYear.TechnicalOfficeUnderCopying
                              + e.CurrentYear.TechnicalOfficeSent + e.CurrentYear.TechnicalOfficeUnderCopying;
            branch += e.PreviousYear.BranchSent + e.PreviousYear.BranchUnderCopying
                     + e.CurrentYear.BranchSent + e.CurrentYear.BranchUnderCopying;
            central += e.PreviousYear.CentralAdministrations + e.CurrentYear.CentralAdministrations;
            followUp += e.PreviousYear.FollowUpCases + e.CurrentYear.FollowUpCases;
            spatial += e.PreviousYear.SpatialVariableCases + e.CurrentYear.SpatialVariableCases;
        }

        // IMPORTANT: computed from aggregated totals, never by averaging per-employee percentages (spec section 25).
        var overallAvailable = prevTotal + currRegistered;
        var overallCompleted = prevCompleted + currCompleted;
        var overallAchievement = SafeDivide(overallCompleted, overallAvailable);

        return new ReportTotalsDto(
            TotalPreviousYearCases: prevTotal,
            TotalPreviousYearCompleted: prevCompleted,
            TotalPreviousYearRemaining: prevRemaining,
            TotalCurrentYearReceived: currReceived,
            TotalCurrentYearRegistered: currRegistered,
            TotalCurrentYearCompleted: currCompleted,
            TotalCurrentYearRemaining: currRemaining,
            TotalPendingDecision: pending,
            TotalTechnicalOffice: technicalOffice,
            TotalBranch: branch,
            TotalCentralAdministrations: central,
            TotalFollowUpCases: followUp,
            TotalSpatialVariableCases: spatial,
            OverallAchievementPercentage: overallAchievement,
            OverallAchievementPercentageFormatted: FormatPercentage(overallAchievement));
    }

    /// <summary>
    /// Never returns NaN or Infinity (spec section 6): denominator <= 0 => 0.
    /// </summary>
    private static double SafeDivide(int numerator, int denominator)
    {
        if (denominator <= 0) return 0d;
        var result = (double)numerator / denominator;
        if (double.IsNaN(result) || double.IsInfinity(result)) return 0d;
        return result;
    }

    private static string FormatPercentage(double ratio) => $"{Math.Round(ratio * 100, 2):0.##}%";

    /// <summary>
    /// Soft, display-level error detection (spec section 11) - "هل يوجد خطأ؟".
    /// These are flagged for review, distinct from the hard input validation in
    /// StatisticsValidators (which rejects impossible values like negatives outright).
    /// </summary>
    private static List<string> DetectErrors(
        PreviousYearStatisticsDto prev,
        CurrentYearStatisticsDto curr,
        int totalAvailable,
        int totalCompleted,
        Employee? employee)
    {
        var errors = new List<string>();

        if (prev.CompletedCases > prev.TotalPreviousYearCases)
            errors.Add("عدد القضايا المنتهية (العام السابق) أكبر من إجمالي القضايا");
        if (prev.RemainingCases < 0)
            errors.Add("عدد القضايا المتبقية (العام السابق) بالسالب");

        if (curr.CompletedCases > curr.TotalRegisteredCases)
            errors.Add("عدد القضايا المنتهية (العام الحالي) أكبر من إجمالي القضايا");
        if (curr.RemainingCases < 0)
            errors.Add("عدد القضايا المتبقية (العام الحالي) بالسالب");

        if (totalCompleted > totalAvailable)
            errors.Add("إجمالي القضايا المنتهية أكبر من إجمالي القضايا المتاحة للتصرف");

        if (employee is null)
            errors.Add("بيانات العضو غير مكتملة");

        return errors;
    }
}
