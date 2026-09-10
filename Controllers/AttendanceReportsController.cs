using HRAttendance.Api.Authorization;
using HRAttendance.Api.Dtos;
using HRAttendance.Api.Models;
using HRAttendance.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRAttendance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AttendanceReportsController : ControllerBase
{
    private readonly IAttendanceReportService _reportService;
    private readonly IAttendanceReportPdfService _pdfService;

    public AttendanceReportsController(IAttendanceReportService reportService, IAttendanceReportPdfService pdfService)
    {
        _reportService = reportService;
        _pdfService = pdfService;
    }

    [HttpGet]
    [RequirePermission("Reports.View")]
    public async Task<ActionResult<AttendanceReportResponse>> GetReport([FromQuery] AttendanceReportRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await _reportService.GetReportAsync(request, cancellationToken)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("daily")]
    [RequirePermission("Reports.View")]
    public async Task<ActionResult<AttendanceReportResponse>> GetDailyReport([FromQuery] DateOnly date, [FromQuery] int? employeeId, [FromQuery] string? department, CancellationToken cancellationToken)
    {
        if (date == default) return BadRequest(new { message = "date is required." });
        return Ok(await _reportService.GetReportAsync(new AttendanceReportRequest { FromDate = date, ToDate = date, EmployeeId = employeeId, Department = department }, cancellationToken));
    }

    [HttpGet("late")]
    [RequirePermission("Reports.View")]
    public async Task<ActionResult<AttendanceReportResponse>> GetLateReport([FromQuery] AttendanceReportRequest request, CancellationToken cancellationToken)
    {
        request.LateOnly = true;
        try { return Ok(await _reportService.GetReportAsync(request, cancellationToken)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("employee/{employeeId:int}")]
    [RequirePermission("Reports.View")]
    public async Task<ActionResult<AttendanceReportResponse>> GetEmployeeReport(int employeeId, [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, CancellationToken cancellationToken)
    {
        try
        {
            var report = await _reportService.GetReportAsync(new AttendanceReportRequest { FromDate = fromDate, ToDate = toDate, EmployeeId = employeeId }, cancellationToken);
            return report.EmployeeCount == 0 ? NotFound(new { message = "Employee not found." }) : Ok(report);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("actions")]
    [RequirePermission("Reports.View")]
    public async Task<ActionResult<IReadOnlyList<AttendanceActionDto>>> GetActions([FromQuery] AttendanceReportRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await _reportService.GetActionsAsync(request, cancellationToken)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("pdf")]
    [RequirePermission("Reports.View")]
    public async Task<IActionResult> ExportPdf([FromQuery] AttendanceReportRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var report = await _reportService.GetReportAsync(request, cancellationToken);
            return File(_pdfService.Generate(report), "application/pdf", $"attendance-report-{report.FromDate:yyyyMMdd}-{report.ToDate:yyyyMMdd}.pdf");
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("pdf/employee/{employeeId:int}")]
    [RequirePermission("Reports.View")]
    public async Task<IActionResult> ExportEmployeePdf(int employeeId, [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, CancellationToken cancellationToken)
    {
        try
        {
            var report = await _reportService.GetReportAsync(new AttendanceReportRequest { FromDate = fromDate, ToDate = toDate, EmployeeId = employeeId }, cancellationToken);
            if (report.EmployeeCount == 0) return NotFound(new { message = "Employee not found." });
            var employeeName = report.Employees[0].FullName.Replace(" ", "-", StringComparison.Ordinal).Replace("/", "-", StringComparison.Ordinal);
            return File(_pdfService.Generate(report), "application/pdf", $"attendance-{employeeName}-{report.FromDate:yyyyMMdd}-{report.ToDate:yyyyMMdd}.pdf");
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
