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

    public AttendanceController(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    // POST /api/attendance/checkin
    // Records "حاضر" for the given (or today's) date and computes LateMinutes
    // against the configured official start time (Attendance:OfficialStartTime
    // in appsettings.json, default 08:30).
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

        record.Status = DayStatus.Present;
        record.CheckIn = time;
        record.LateMinutes = time > officialStart ? (int)(time - officialStart).TotalMinutes : 0;

        await _db.SaveChangesAsync();
        return Ok(record);
    }

    // POST /api/attendance/checkout
    [HttpPost("checkout")]
    [RequirePermission("Attendance.Manage")]
    public async Task<IActionResult> CheckOut(CheckOutRequest request)
    {
        var date = request.Date ?? DateOnly.FromDateTime(DateTime.Today);
        var time = request.Time ?? TimeOnly.FromDateTime(DateTime.Now);

        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == request.EmployeeId && r.Date == date);

        if (record is null)
            return NotFound("No check-in found for this employee/date yet.");

        record.CheckOut = time;
        await _db.SaveChangesAsync();
        return Ok(record);
    }

    // POST /api/attendance/mark
    // Directly sets a day's status. Use this for إجازة اعتيادية / إجازة عارضة /
    // إجازة مرضية / انقطاع, or to manually correct a day back to "present"/"none".
    // (For مأمورية use POST /api/missions, for إذن use POST /api/permission-requests -
    // those also create their own Mission/PermissionRequest record.)
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
        record.Status = status.Value;

        // Marking a day as leave/absent clears any stale check-in/out/lateness.
        if (status != DayStatus.Present)
        {
            record.CheckIn = null;
            record.CheckOut = null;
            record.LateMinutes = 0;
        }

        await _db.SaveChangesAsync();
        return Ok(record);
    }

    // POST /api/attendance/lateness
    // Directly corrects the recorded lateness (minutes) for a day that already
    // has an attendance record.
    [HttpPost("lateness")]
    [RequirePermission("Attendance.Manage")]
    public async Task<IActionResult> RecordLateness(RecordLatenessRequest request)
    {
        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == request.EmployeeId && r.Date == request.Date);

        if (record is null) return NotFound("No attendance record exists for this employee/date yet.");

        record.LateMinutes = request.Minutes;
        await _db.SaveChangesAsync();
        return Ok(record);
    }
}
