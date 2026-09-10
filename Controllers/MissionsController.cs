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
public class MissionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    public MissionsController(AppDbContext db, IAuditService audit) { _db = db; _audit = audit; }

    private int? CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpPost]
    [RequirePermission("Attendance.Manage")]
    public async Task<ActionResult<MissionDto>> Create(CreateMissionRequest request)
    {
        var employeeExists = await _db.Employees.AnyAsync(e => e.Id == request.EmployeeId);
        if (!employeeExists) return NotFound("Employee not found.");

        var mission = new Mission { EmployeeId = request.EmployeeId, Date = request.Date, Reason = request.Reason, Location = request.Location };
        _db.Missions.Add(mission);
        var record = await AttendanceHelper.GetOrCreateAsync(_db, request.EmployeeId, request.Date);
        record.Status = DayStatus.Mission;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(CurrentUserId, "Mission.Create", "Mission", mission.Id, null, new { mission.Id, mission.EmployeeId, mission.Date, mission.Reason, mission.Location });
        await _audit.NotifyAsync($"تم تسجيل مأمورية للموظف رقم {mission.EmployeeId} بتاريخ {mission.Date:yyyy-MM-dd}.");

        return Ok(new MissionDto { Id = mission.Id, Date = mission.Date, Reason = mission.Reason, Location = mission.Location });
    }
}
