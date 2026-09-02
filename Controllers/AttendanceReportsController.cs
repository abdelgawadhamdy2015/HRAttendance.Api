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

    public AttendanceReportsController(
        IAttendanceReportService reportService,
        IAttendanceReportPdfService pdfService)
    {
        _reportService = reportService;
        _pdfService = pdfService;
    }

    // GET /api/attendancereports?fromDate=2024-05-01&toDate=2024-05-31
    [HttpGet]
    public async Task<ActionResult<AttendanceReportResponse>> GetReport(
        [FromQuery] AttendanceReportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _reportService.GetReportAsync(request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET /api/attendancereports/daily?date=2024-05-30
    [HttpGet("daily")]
    public async Task<ActionResult<AttendanceReportResponse>> GetDailyReport(
        [FromQuery] DateOnly date,
        [FromQuery] int? employeeId,
        [FromQuery] string? department,
        CancellationToken cancellationToken)
    {
        if (date == default)
            return BadRequest(new { message = "date is required." });

        var request = new AttendanceReportRequest
        {
            FromDate = date,
            ToDate = date,
            EmployeeId = employeeId,
            Department = department
        };

        return Ok(await _reportService.GetReportAsync(request, cancellationToken));
    }

    // GET /api/attendancereports/late?fromDate=2024-05-01&toDate=2024-05-31
    [HttpGet("late")]
    public async Task<ActionResult<AttendanceReportResponse>> GetLateReport(
        [FromQuery] AttendanceReportRequest request,
        CancellationToken cancellationToken)
    {
        request.LateOnly = true;

        try
        {
            return Ok(await _reportService.GetReportAsync(request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET /api/attendancereports/employee/1?fromDate=2024-05-01&toDate=2024-05-31
    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult<AttendanceReportResponse>> GetEmployeeReport(
        int employeeId,
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        CancellationToken cancellationToken)
    {
        var request = new AttendanceReportRequest
        {
            FromDate = fromDate,
            ToDate = toDate,
            EmployeeId = employeeId
        };

        try
        {
            var report = await _reportService.GetReportAsync(request, cancellationToken);
            if (report.EmployeeCount == 0)
                return NotFound(new { message = "Employee not found." });

            return Ok(report);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET /api/attendancereports/actions?fromDate=2024-05-01&toDate=2024-05-31
    [HttpGet("actions")]
    public async Task<ActionResult<IReadOnlyList<AttendanceActionDto>>> GetActions(
        [FromQuery] AttendanceReportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _reportService.GetActionsAsync(request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET /api/attendancereports/pdf?fromDate=2024-05-01&toDate=2024-05-31
    [HttpGet("pdf")]
    public async Task<IActionResult> ExportPdf(
        [FromQuery] AttendanceReportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var report = await _reportService.GetReportAsync(request, cancellationToken);
            var pdf = _pdfService.Generate(report);
            var fileName = $"attendance-report-{report.FromDate:yyyyMMdd}-{report.ToDate:yyyyMMdd}.pdf";

            return File(pdf, "application/pdf", fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET /api/attendancereports/pdf/employee/1?fromDate=2024-05-01&toDate=2024-05-31
    [HttpGet("pdf/employee/{employeeId:int}")]
    public async Task<IActionResult> ExportEmployeePdf(
        int employeeId,
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        CancellationToken cancellationToken)
    {
        var request = new AttendanceReportRequest
        {
            FromDate = fromDate,
            ToDate = toDate,
            EmployeeId = employeeId
        };

        try
        {
            var report = await _reportService.GetReportAsync(request, cancellationToken);
            if (report.EmployeeCount == 0)
                return NotFound(new { message = "Employee not found." });

            var pdf = _pdfService.Generate(report);
            var employeeName = report.Employees[0].FullName
                .Replace(" ", "-", StringComparison.Ordinal)
                .Replace("/", "-", StringComparison.Ordinal);
            var fileName = $"attendance-{employeeName}-{report.FromDate:yyyyMMdd}-{report.ToDate:yyyyMMdd}.pdf";

            return File(pdf, "application/pdf", fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
