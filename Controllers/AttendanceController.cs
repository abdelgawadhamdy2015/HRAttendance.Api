using System.Security.Claims;
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
public class AttendanceController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IAuditService _audit;

    public AttendanceController(AppDbContext db, IConfiguration configuration, IAuditService audit)
    {
        _db = db;
        _configuration = configuration;
        _audit = audit;
    }

    private int? CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpPost("checkin")]
    [RequirePermission("Attendance.Manage")]
    public async Task<ActionResult<AttendanceRecord>> CheckIn(CheckInRequest request)
    {
        var employeeExists = await _db.Employees.AnyAsync(e => e.Id == request.EmployeeId);
        if (!employeeExists) return NotFound("Employee not found.");

        var date = request.Date ?? DateOnly.FromDateTime(DateTime.Today);
        var time = request.Time ?? TimeOnly.FromDateTime(DateTime.Now);
        var record = await AttendanceHelper.GetOrCreateAsync(_db, request.EmployeeId, date);

        var officialStartText = _configuration["Attendance:OfficialStartTime"] ?? "08:30";
        var officialStart = TimeOnly.Parse(officialStartText);
        var old = new { record.Status, record.CheckIn, record.CheckOut, record.LateMinutes };

        record.Status = DayStatus.Present;
        record.CheckIn = time;
        record.LateMinutes = time > officialStart ? (int)(time - officialStart).TotalMinutes : 0;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(CurrentUserId, "Attendance.CheckIn", "AttendanceRecord", record.Id, old,
            new { record.Status, record.CheckIn, record.CheckOut, record.LateMinutes });
        if (record.LateMinutes > 0)
            await _audit.NotifyAsync($"تم تسجيل حضور متأخر للموظف رقم {request.EmployeeId}: {record.LateMinutes} دقيقة.", NotificationSeverity.Warning);

        return Ok(record);
    }

    [HttpPost("checkout")]
    [RequirePermission("Attendance.Manage")]
    public async Task<IActionResult> CheckOut(CheckOutRequest request)
    {
        var date = request.Date ?? DateOnly.FromDateTime(DateTime.Today);
        var time = request.Time ?? TimeOnly.FromDateTime(DateTime.Now);
        var record = await _db.AttendanceRecords.FirstOrDefaultAsync(r => r.EmployeeId == request.EmployeeId && r.Date == date);
        if (record is null) return NotFound("No check-in found for this employee/date yet.");

        var old = record.CheckOut;
        record.CheckOut = time;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, "Attendance.CheckOut", "AttendanceRecord", record.Id,
            new { checkOut = old }, new { checkOut = record.CheckOut });
        return Ok(record);
    }

    [HttpPost("mark")]
    [RequirePermission("Attendance.Manage")]
    public async Task<IActionResult> MarkDay(MarkAttendanceRequest request)
    {
        var employeeExists = await _db.Employees.AnyAsync(e => e.Id == request.EmployeeId);
        if (!employeeExists) return NotFound("Employee not found.");

        var status = AttendanceHelper.StringToStatus(request.Status);
        if (status is null || status is DayStatus.Mission or DayStatus.Permission)
            return BadRequest("Status must be one of: present, annualLeave, casualLeave, sickLeave, cutOff, none.");

        var record = await AttendanceHelper.GetOrCreateAsync(_db, request.EmployeeId, request.Date);
        var old = new { record.Status, record.CheckIn, record.CheckOut, record.LateMinutes };
        record.Status = status.Value;
        if (status != DayStatus.Present)
        {
            record.CheckIn = null;
            record.CheckOut = null;
            record.LateMinutes = 0;
        }
        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, "Attendance.MarkDay", "AttendanceRecord", record.Id, old,
            new { record.Status, record.CheckIn, record.CheckOut, record.LateMinutes });
        return Ok(record);
    }

    [HttpPost("lateness")]
    [RequirePermission("Attendance.Manage")]
    public async Task<IActionResult> RecordLateness(RecordLatenessRequest request)
    {
        if (request.Minutes < 0) return BadRequest("Minutes cannot be negative.");
        var record = await _db.AttendanceRecords.FirstOrDefaultAsync(r => r.EmployeeId == request.EmployeeId && r.Date == request.Date);
        if (record is null) return NotFound("No attendance record exists for this employee/date yet.");

        var old = record.LateMinutes;
        record.LateMinutes = request.Minutes;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, "Attendance.RecordLateness", "AttendanceRecord", record.Id,
            new { minutes = old }, new { minutes = record.LateMinutes });
        return Ok(record);
    }
}
