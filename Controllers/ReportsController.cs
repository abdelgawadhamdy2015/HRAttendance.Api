using FluentValidation;
using HRAttendance.Api.Authorization;
using HRAttendance.Api.Data;
using HRAttendance.Api.Dtos;
using HRAttendance.Api.Models;
using HRAttendance.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRAttendance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IStatisticsCalculationService _calc;
    private readonly IAuditService _audit;
    private readonly IValidator<UpdateEmployeeStatisticsRequest> _statsValidator;

    public ReportsController(
        AppDbContext db,
        IStatisticsCalculationService calc,
        IAuditService audit,
        IValidator<UpdateEmployeeStatisticsRequest> statsValidator)
    {
        _db = db;
        _calc = calc;
        _audit = audit;
        _statsValidator = statsValidator;
    }

    // GET /api/reports?pageNumber=1&pageSize=20&year=2025&month=10&search=
    [HttpGet]
    public async Task<ActionResult<PagedResult<ReportSummaryDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        [FromQuery] string? search = null)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _db.Reports.AsNoTracking().AsQueryable();
        if (year.HasValue) query = query.Where(r => r.Year == year.Value);
        if (month.HasValue) query = query.Where(r => r.Month == month.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.OfficeNameArabic.Contains(search) || r.OfficeNameEnglish.Contains(search));

        var totalCount = await query.CountAsync();

        var reports = await query
            .OrderByDescending(r => r.Year).ThenByDescending(r => r.Month)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.Year,
                r.Month,
                r.OfficeNameArabic,
                r.OfficeNameEnglish,
                r.IsFinalized,
                r.FinalizedAt,
                r.CreatedAt,
                EmployeeCount = r.EmployeeReports.Count,
                ErrorCount = r.EmployeeReports.Count(er => er.HasError)
            })
            .ToListAsync();

        var items = reports.Select(r => new ReportSummaryDto(
            r.Id, r.Year, r.Month, r.OfficeNameArabic, r.OfficeNameEnglish,
            r.IsFinalized, r.FinalizedAt, r.EmployeeCount, r.ErrorCount, r.CreatedAt)).ToList();

        return Ok(new PagedResult<ReportSummaryDto>(items, pageNumber, pageSize, totalCount));
    }

    // GET /api/reports/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReportSummaryDto>> GetById(int id)
    {
        var r = await _db.Reports
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Year,
                x.Month,
                x.OfficeNameArabic,
                x.OfficeNameEnglish,
                x.IsFinalized,
                x.FinalizedAt,
                x.CreatedAt,
                EmployeeCount = x.EmployeeReports.Count,
                ErrorCount = x.EmployeeReports.Count(er => er.HasError)
            })
            .FirstOrDefaultAsync();

        if (r is null) return NotFound();

        return Ok(new ReportSummaryDto(
            r.Id, r.Year, r.Month, r.OfficeNameArabic, r.OfficeNameEnglish,
            r.IsFinalized, r.FinalizedAt, r.EmployeeCount, r.ErrorCount, r.CreatedAt));
    }

    // POST /api/reports
    [HttpPost]
    [RequirePermission("Reports.Manage")]
    public async Task<ActionResult<ReportSummaryDto>> Create(CreateReportRequest request)
    {
        if (request.Month is < 1 or > 12)
            return BadRequest(new { message = "الشهر يجب أن يكون بين 1 و 12" });
        if (request.Year < 2000 || request.Year > 2100)
            return BadRequest(new { message = "السنة غير صالحة" });

        var officeAr = string.IsNullOrWhiteSpace(request.OfficeNameArabic)
            ? "نيابة الأزهر الإدارية" : request.OfficeNameArabic!;
        var officeEn = string.IsNullOrWhiteSpace(request.OfficeNameEnglish)
            ? "Al-Azhar Administrative Prosecution" : request.OfficeNameEnglish!;

        var duplicate = await _db.Reports.AnyAsync(r =>
            r.Year == request.Year && r.Month == request.Month && r.OfficeNameArabic == officeAr);
        if (duplicate)
            return Conflict(new { message = "يوجد تقرير بنفس الشهر والسنة لهذا المكتب بالفعل" });

        var report = new Report
        {
            Year = request.Year,
            Month = request.Month,
            OfficeNameArabic = officeAr,
            OfficeNameEnglish = officeEn
        };
        _db.Reports.Add(report);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(this.GetCurrentUserId(), "Report.Create", "Report", report.Id, newValue: report);

        return CreatedAtAction(nameof(GetById), new { id = report.Id },
            new ReportSummaryDto(report.Id, report.Year, report.Month, report.OfficeNameArabic,
                report.OfficeNameEnglish, report.IsFinalized, report.FinalizedAt, 0, 0, report.CreatedAt));
    }

    // PUT /api/reports/5
    [HttpPut("{id:int}")]
    [RequirePermission("Reports.Manage")]
    public async Task<IActionResult> Update(int id, UpdateReportRequest request)
    {
        var report = await _db.Reports.FindAsync(id);
        if (report is null) return NotFound();
        if (report.IsFinalized) return Conflict(new { message = "لا يمكن تعديل تقرير معتمد" });

        if (!string.IsNullOrWhiteSpace(request.OfficeNameArabic)) report.OfficeNameArabic = request.OfficeNameArabic;
        if (!string.IsNullOrWhiteSpace(request.OfficeNameEnglish)) report.OfficeNameEnglish = request.OfficeNameEnglish;
        report.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync(this.GetCurrentUserId(), "Report.Update", "Report", report.Id, newValue: report);
        return NoContent();
    }

    // DELETE /api/reports/5 - drafts only
    [HttpDelete("{id:int}")]
    [RequirePermission("Reports.Manage")]
    public async Task<IActionResult> Delete(int id)
    {
        var report = await _db.Reports.FindAsync(id);
        if (report is null) return NotFound();
        if (report.IsFinalized) return Conflict(new { message = "لا يمكن حذف تقرير معتمد" });

        _db.Reports.Remove(report);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(this.GetCurrentUserId(), "Report.Delete", "Report", id);
        return NoContent();
    }

    // POST /api/reports/5/finalize
    [HttpPost("{id:int}/finalize")]
    [RequirePermission("Reports.Finalize")]
    public async Task<IActionResult> Finalize(int id)
    {
        var report = await _db.Reports.FindAsync(id);
        if (report is null) return NotFound();
        if (report.IsFinalized) return Conflict(new { message = "التقرير معتمد بالفعل" });

        report.IsFinalized = true;
        report.FinalizedAt = DateTime.UtcNow;
        report.FinalizedByUserId = this.GetCurrentUserId();
        await _db.SaveChangesAsync();
        await _audit.LogAsync(this.GetCurrentUserId(), "Report.Finalize", "Report", report.Id);
        return NoContent();
    }

    // POST /api/reports/5/reopen - admin only
    [HttpPost("{id:int}/reopen")]
    [RequirePermission("Reports.Reopen")]
    public async Task<IActionResult> Reopen(int id)
    {
        var report = await _db.Reports.FindAsync(id);
        if (report is null) return NotFound();
        if (!report.IsFinalized) return Conflict(new { message = "التقرير غير معتمد أصلاً" });

        report.IsFinalized = false;
        report.FinalizedAt = null;
        report.FinalizedByUserId = null;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(this.GetCurrentUserId(), "Report.Reopen", "Report", report.Id);
        return NoContent();
    }

    // POST /api/reports/5/employees - add an employee to the report with zeroed statistics
    [HttpPost("{id:int}/employees")]
    [RequirePermission("Reports.Manage")]
    public async Task<ActionResult<EmployeeReportStatisticsDto>> AddEmployee(int id, AddEmployeeToReportRequest request)
    {
        var report = await _db.Reports.FindAsync(id);
        if (report is null) return NotFound(new { message = "التقرير غير موجود" });
        if (report.IsFinalized) return Conflict(new { message = "لا يمكن تعديل تقرير معتمد" });

        var employee = await _db.Employees.FindAsync(request.EmployeeId);
        if (employee is null) return NotFound(new { message = "العضو غير موجود" });

        var exists = await _db.EmployeeReports.AnyAsync(er => er.ReportId == id && er.EmployeeId == request.EmployeeId);
        if (exists) return Conflict(new { message = "العضو مضاف بالفعل لهذا التقرير" });

        var er = new EmployeeReport
        {
            ReportId = id,
            EmployeeId = request.EmployeeId,
            LeaveReason = request.LeaveReason,
            Notes = request.Notes,
            PreviousYearStatistics = new PreviousYearStatistics(),
            CurrentYearStatistics = new CurrentYearStatistics()
        };
        _db.EmployeeReports.Add(er);
        await _db.SaveChangesAsync();

        // reload with navigation properties for calculation
        var reloaded = await _db.EmployeeReports
            .Include(x => x.Employee)
            .Include(x => x.PreviousYearStatistics)
            .Include(x => x.CurrentYearStatistics)
            .FirstAsync(x => x.Id == er.Id);

        await _audit.LogAsync(this.GetCurrentUserId(), "Report.AddEmployee", "EmployeeReport", er.Id);
        return Ok(_calc.Calculate(reloaded));
    }

    // DELETE /api/reports/5/employees/12
    [HttpDelete("{id:int}/employees/{employeeId:int}")]
    [RequirePermission("Reports.Manage")]
    public async Task<IActionResult> RemoveEmployee(int id, int employeeId)
    {
        var report = await _db.Reports.FindAsync(id);
        if (report is null) return NotFound();
        if (report.IsFinalized) return Conflict(new { message = "لا يمكن تعديل تقرير معتمد" });

        var er = await _db.EmployeeReports.FirstOrDefaultAsync(x => x.ReportId == id && x.EmployeeId == employeeId);
        if (er is null) return NotFound();

        _db.EmployeeReports.Remove(er);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(this.GetCurrentUserId(), "Report.RemoveEmployee", "EmployeeReport", er.Id);
        return NoContent();
    }

    // GET /api/reports/5/statistics - full computed table + totals row (spec sections 9, 25)
    [HttpGet("{id:int}/statistics")]
    public async Task<ActionResult<ReportStatisticsResponse>> GetStatistics(
        int id,
        [FromQuery] string? search = null,
        [FromQuery] string? grade = null)
    {
        var report = await _db.Reports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        if (report is null) return NotFound();

        var query = _db.EmployeeReports
            .AsNoTracking()
            .Include(er => er.Employee)
            .Include(er => er.PreviousYearStatistics)
            .Include(er => er.CurrentYearStatistics)
            .Where(er => er.ReportId == id);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(er =>
                (er.Employee!.NameArabic != null && er.Employee.NameArabic.Contains(search)) ||
                (er.Employee!.NameEnglish != null && er.Employee.NameEnglish.Contains(search)) ||
                er.Employee!.FullName.Contains(search));

        if (!string.IsNullOrWhiteSpace(grade))
            query = query.Where(er => er.Employee!.GradeArabic == grade);

        var employeeReports = await query.ToListAsync();
        var employeeStats = employeeReports.Select(_calc.Calculate).ToList();
        var totals = _calc.CalculateTotals(employeeStats);

        return Ok(new ReportStatisticsResponse(report.Id, report.Year, report.Month, report.IsFinalized, employeeStats, totals));
    }

    // GET /api/reports/5/employees/12 - single employee's computed statistics
    [HttpGet("{id:int}/employees/{employeeId:int}")]
    public async Task<ActionResult<EmployeeReportStatisticsDto>> GetEmployeeStatistics(int id, int employeeId)
    {
        var er = await _db.EmployeeReports
            .AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.PreviousYearStatistics)
            .Include(x => x.CurrentYearStatistics)
            .FirstOrDefaultAsync(x => x.ReportId == id && x.EmployeeId == employeeId);

        if (er is null) return NotFound();
        return Ok(_calc.Calculate(er));
    }

    // PUT /api/reports/5/employees/12/statistics - the main data-entry endpoint (spec section 10)
    [HttpPut("{id:int}/employees/{employeeId:int}/statistics")]
    [RequirePermission("Statistics.Edit")]
    public async Task<ActionResult<EmployeeReportStatisticsDto>> UpdateEmployeeStatistics(
        int id, int employeeId, UpdateEmployeeStatisticsRequest request)
    {
        var validation = await _statsValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });

        var report = await _db.Reports.FindAsync(id);
        if (report is null) return NotFound(new { message = "التقرير غير موجود" });
        if (report.IsFinalized) return Conflict(new { message = "لا يمكن تعديل تقرير معتمد" });

        var er = await _db.EmployeeReports
            .Include(x => x.Employee)
            .Include(x => x.PreviousYearStatistics)
            .Include(x => x.CurrentYearStatistics)
            .FirstOrDefaultAsync(x => x.ReportId == id && x.EmployeeId == employeeId);

        if (er is null) return NotFound(new { message = "العضو غير مضاف لهذا التقرير" });

        var before = _calc.Calculate(er);

        er.LeaveReason = request.LeaveReason;
        er.Notes = request.Notes;

        var p = request.PreviousYear;
        er.PreviousYearStatistics ??= new PreviousYearStatistics { EmployeeReportId = er.Id };
        er.PreviousYearStatistics.PendingDecision = p.PendingDecision;
        er.PreviousYearStatistics.TotalPreviousYearCases = p.TotalPreviousYearCases;
        er.PreviousYearStatistics.CompletedCases = p.CompletedCases;
        er.PreviousYearStatistics.UnderInvestigation = p.UnderInvestigation;
        er.PreviousYearStatistics.TechnicalOfficeSent = p.TechnicalOfficeSent;
        er.PreviousYearStatistics.TechnicalOfficeUnderCopying = p.TechnicalOfficeUnderCopying;
        er.PreviousYearStatistics.BranchSent = p.BranchSent;
        er.PreviousYearStatistics.BranchUnderCopying = p.BranchUnderCopying;
        er.PreviousYearStatistics.CentralAdministrations = p.CentralAdministrations;
        er.PreviousYearStatistics.FollowUpCases = p.FollowUpCases;
        er.PreviousYearStatistics.SpatialVariableCases = p.SpatialVariableCases;

        var c = request.CurrentYear;
        er.CurrentYearStatistics ??= new CurrentYearStatistics { EmployeeReportId = er.Id };
        er.CurrentYearStatistics.TransferredFromBeginningOfYear = c.TransferredFromBeginningOfYear;
        er.CurrentYearStatistics.ReceivedDuringMonth = c.ReceivedDuringMonth;
        er.CurrentYearStatistics.CompletedCases = c.CompletedCases;
        er.CurrentYearStatistics.UnderInvestigation = c.UnderInvestigation;
        er.CurrentYearStatistics.TechnicalOfficeSent = c.TechnicalOfficeSent;
        er.CurrentYearStatistics.TechnicalOfficeUnderCopying = c.TechnicalOfficeUnderCopying;
        er.CurrentYearStatistics.BranchSent = c.BranchSent;
        er.CurrentYearStatistics.BranchUnderCopying = c.BranchUnderCopying;
        er.CurrentYearStatistics.CentralAdministrations = c.CentralAdministrations;
        er.CurrentYearStatistics.FollowUpCases = c.FollowUpCases;
        er.CurrentYearStatistics.SpatialVariableCases = c.SpatialVariableCases;
        er.CurrentYearStatistics.ReceivedFromOtherProsecutions = c.ReceivedFromOtherProsecutions;
        er.CurrentYearStatistics.PendingDecision = c.PendingDecision;

        var after = _calc.Calculate(er);
        er.HasError = after.HasError; // cached for fast list queries only

        await _db.SaveChangesAsync();
        await _audit.LogAsync(this.GetCurrentUserId(), "Statistics.Update", "EmployeeReport", er.Id, oldValue: before, newValue: after);

        return Ok(after);
    }
}
