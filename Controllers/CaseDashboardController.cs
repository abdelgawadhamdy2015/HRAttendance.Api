using HRAttendance.Api.Data;
using HRAttendance.Api.Dtos;
using HRAttendance.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRAttendance.Api.Controllers;

[ApiController]
[Route("api/case-dashboard")]
[Authorize]
public class CaseDashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IStatisticsCalculationService _calc;

    public CaseDashboardController(AppDbContext db, IStatisticsCalculationService calc)
    {
        _db = db;
        _calc = calc;
    }

    // GET /api/case-dashboard?reportId=5
    // GET /api/case-dashboard?year=2025&month=10   (resolves the matching report)
    [HttpGet]
    public async Task<ActionResult<CaseDashboardResponse>> Get(
        [FromQuery] int? reportId,
        [FromQuery] int? year,
        [FromQuery] int? month)
    {
        var reportQuery = _db.Reports.AsNoTracking().AsQueryable();
        if (reportId.HasValue) reportQuery = reportQuery.Where(r => r.Id == reportId.Value);
        else if (year.HasValue && month.HasValue) reportQuery = reportQuery.Where(r => r.Year == year.Value && r.Month == month.Value);
        else reportQuery = reportQuery.OrderByDescending(r => r.Year).ThenByDescending(r => r.Month);

        var report = await reportQuery.FirstOrDefaultAsync();
        if (report is null) return NotFound(new { message = "لا يوجد تقرير مطابق" });

        var employeeReports = await _db.EmployeeReports
            .AsNoTracking()
            .Include(er => er.Employee)
            .Include(er => er.PreviousYearStatistics)
            .Include(er => er.CurrentYearStatistics)
            .Where(er => er.ReportId == report.Id)
            .ToListAsync();

        var stats = employeeReports.Select(_calc.Calculate).ToList();
        var totals = _calc.CalculateTotals(stats);

        var kpis = new DashboardKpisDto(
            TotalCases: totals.TotalPreviousYearCases + totals.TotalCurrentYearRegistered,
            CompletedCases: totals.TotalPreviousYearCompleted + totals.TotalCurrentYearCompleted,
            RemainingCases: totals.TotalPreviousYearRemaining + totals.TotalCurrentYearRemaining,
            CurrentYearCases: totals.TotalCurrentYearRegistered,
            PreviousYearCases: totals.TotalPreviousYearCases,
            AchievementPercentage: totals.OverallAchievementPercentage,
            AchievementPercentageFormatted: totals.OverallAchievementPercentageFormatted,
            ReceivedDuringMonth: totals.TotalCurrentYearReceived,
            ReceivedFromOtherProsecutions: stats.Sum(s => s.CurrentYear.ReceivedFromOtherProsecutions));

        var achievementPerEmployee = stats
            .OrderByDescending(s => s.AchievementPercentage)
            .Select(s => new EmployeeAchievementDto(s.EmployeeId, s.EmployeeNameArabic, s.AchievementPercentage, s.TotalCasesAvailableForProcessing))
            .ToList();

        var casesPerEmployee = stats
            .OrderByDescending(s => s.TotalCasesAvailableForProcessing)
            .Select(s => new EmployeeAchievementDto(s.EmployeeId, s.EmployeeNameArabic, s.AchievementPercentage, s.TotalCasesAvailableForProcessing))
            .ToList();

        return Ok(new CaseDashboardResponse(report.Id, report.Year, report.Month, kpis, achievementPerEmployee, casesPerEmployee));
    }
}
