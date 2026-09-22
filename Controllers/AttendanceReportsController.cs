using System.Security.Claims;
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
public sealed class AttendanceReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAttendanceReportService _reportService;
    private readonly IAttendanceReportPdfService _pdfService;

    public AttendanceReportsController(
        AppDbContext db,
        IAttendanceReportService reportService,
        IAttendanceReportPdfService pdfService)
    {
        _db = db;
        _reportService = reportService;
        _pdfService = pdfService;
    }

    [HttpGet]
    public async Task<ActionResult<AttendanceReportResponse>> GetReport(
        [FromQuery] AttendanceReportRequest request,
        CancellationToken cancellationToken)
    {
        if (!await ApplyReportScopeAsync(request, cancellationToken))
            return Ok(EmptyReport(request));

        try { return Ok(await _reportService.GetReportAsync(request, cancellationToken)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("daily")]
    public async Task<ActionResult<AttendanceReportResponse>> GetDailyReport(
        [FromQuery] DateOnly date,
        [FromQuery] int? employeeId,
        [FromQuery] string? department,
        CancellationToken cancellationToken)
    {
        if (date == default) return BadRequest(new { message = "date is required." });
        var request = new AttendanceReportRequest { FromDate = date, ToDate = date, EmployeeId = employeeId, Department = department };
        if (!await ApplyReportScopeAsync(request, cancellationToken)) return Ok(EmptyReport(request));
        return Ok(await _reportService.GetReportAsync(request, cancellationToken));
    }

    [HttpGet("late")]
    public async Task<ActionResult<AttendanceReportResponse>> GetLateReport(
        [FromQuery] AttendanceReportRequest request,
        CancellationToken cancellationToken)
    {
        request.LateOnly = true;
        if (!await ApplyReportScopeAsync(request, cancellationToken)) return Ok(EmptyReport(request));
        try { return Ok(await _reportService.GetReportAsync(request, cancellationToken)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult<AttendanceReportResponse>> GetEmployeeReport(
        int employeeId,
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        CancellationToken cancellationToken)
    {
        var request = new AttendanceReportRequest { FromDate = fromDate, ToDate = toDate, EmployeeId = employeeId };
        if (!await ApplyReportScopeAsync(request, cancellationToken)) return Forbid();

        try
        {
            var report = await _reportService.GetReportAsync(request, cancellationToken);
            return report.EmployeeCount == 0 ? NotFound(new { message = "Employee not found." }) : Ok(report);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("actions")]
    public async Task<ActionResult<IReadOnlyList<AttendanceActionDto>>> GetActions(
        [FromQuery] AttendanceReportRequest request,
        CancellationToken cancellationToken)
    {
        if (!await ApplyReportScopeAsync(request, cancellationToken)) return Ok(Array.Empty<AttendanceActionDto>());
        try { return Ok(await _reportService.GetActionsAsync(request, cancellationToken)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("pdf")]
    public async Task<IActionResult> ExportPdf(
        [FromQuery] AttendanceReportRequest request,
        CancellationToken cancellationToken)
    {
        if (!await ApplyReportScopeAsync(request, cancellationToken))
            return File(_pdfService.Generate(EmptyReport(request)), "application/pdf", "attendance-report-empty.pdf");

        try
        {
            var report = await _reportService.GetReportAsync(request, cancellationToken);
            return File(_pdfService.Generate(report), "application/pdf", $"attendance-report-{report.FromDate:yyyyMMdd}-{report.ToDate:yyyyMMdd}.pdf");
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("pdf/employee/{employeeId:int}")]
    public async Task<IActionResult> ExportEmployeePdf(
        int employeeId,
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        CancellationToken cancellationToken)
    {
        var request = new AttendanceReportRequest { FromDate = fromDate, ToDate = toDate, EmployeeId = employeeId };
        if (!await ApplyReportScopeAsync(request, cancellationToken)) return Forbid();

        try
        {
            var report = await _reportService.GetReportAsync(request, cancellationToken);
            if (report.EmployeeCount == 0) return NotFound(new { message = "Employee not found." });
            var employeeName = report.Employees[0].FullName.Replace(" ", "-", StringComparison.Ordinal).Replace("/", "-", StringComparison.Ordinal);
            return File(_pdfService.Generate(report), "application/pdf", $"attendance-{employeeName}-{report.FromDate:yyyyMMdd}-{report.ToDate:yyyyMMdd}.pdf");
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    private async Task<bool> ApplyReportScopeAsync(
        AttendanceReportRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return false;

        var permissionNames = (await _db.UserPermissions
      .Where(up => up.UserId == userId.Value)
      .Select(up => up.Permission.Name)
      .ToListAsync(cancellationToken))
      .ToHashSet(StringComparer.Ordinal);

        if (HasViewAllReports(permissionNames))
            return true;

        // Without a report permission, a user can only see the employee linked to their login.
        var ownEmployeeId = await _db.Users
            .Where(u => u.Id == userId.Value)
            .Select(u => u.EmployeeId)
            .FirstOrDefaultAsync(cancellationToken);

        request.EmployeeId = ownEmployeeId;
        request.Department = null;
        return ownEmployeeId.HasValue;
    }

    private static bool HasViewAllReports(IEnumerable<string> permissions)
    {
        var set = permissions.ToHashSet(StringComparer.Ordinal);
        return set.Contains("Reports.View") || set.Contains("Reports.Manage") || set.Contains("Reports.Edit");
    }

    private int? GetCurrentUserId()
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static AttendanceReportResponse EmptyReport(AttendanceReportRequest request) => new()
    {
        FromDate = request.FromDate,
        ToDate = request.ToDate,
        EmployeeCount = 0,
        WorkingDays = request.FromDate == default || request.ToDate == default || request.FromDate > request.ToDate
            ? 0
            : request.ToDate.DayNumber - request.FromDate.DayNumber + 1,
        TotalRecords = 0,
        PresentDays = 0,
        AbsentDays = 0,
        LateDays = 0,
        TotalLateMinutes = 0,
        TotalWorkedMinutes = 0,
        Employees = []
    };
}
