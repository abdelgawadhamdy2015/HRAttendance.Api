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
[Route("api/permission-requests")]
[Authorize]
public class PermissionRequestsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private const int MaxPermissionsPerMonth = 2;

    public PermissionRequestsController(AppDbContext db, IAuditService audit) { _db = db; _audit = audit; }
    private int? CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpPost]
    [RequirePermission("Attendance.Manage")]
    public async Task<ActionResult<PermissionDto>> Create(CreatePermissionRequestDto request)
    {
        var employeeExists = await _db.Employees.AnyAsync(e => e.Id == request.EmployeeId);
        if (!employeeExists) return NotFound("Employee not found.");
        if (!TimeOnly.TryParse(request.From, out var from) || !TimeOnly.TryParse(request.To, out var to)) return BadRequest("From/To must be valid times, e.g. \"11:00\".");
        if (to <= from) return BadRequest("To must be after From.");

        var usedThisMonth = await _db.PermissionRequests.CountAsync(p => p.EmployeeId == request.EmployeeId && p.Date.Year == request.Date.Year && p.Date.Month == request.Date.Month);
        if (usedThisMonth >= MaxPermissionsPerMonth) return Conflict($"Employee has already used {MaxPermissionsPerMonth} permissions this month.");

        var permission = new PermissionRequest { EmployeeId = request.EmployeeId, Date = request.Date, From = from, To = to, Reason = request.Reason };
        _db.PermissionRequests.Add(permission);
        var record = await AttendanceHelper.GetOrCreateAsync(_db, request.EmployeeId, request.Date);
        record.Status = DayStatus.Permission;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(CurrentUserId, "PermissionRequest.Create", "PermissionRequest", permission.Id, null, new { permission.Id, permission.EmployeeId, permission.Date, permission.From, permission.To, permission.Reason });
        await _audit.NotifyAsync($"تم تسجيل إذن للموظف رقم {permission.EmployeeId} بتاريخ {permission.Date:yyyy-MM-dd}.");
        return Ok(new PermissionDto { Id = permission.Id, Date = permission.Date, From = permission.From.ToString("HH:mm"), To = permission.To.ToString("HH:mm"), Reason = permission.Reason });
    }
}
