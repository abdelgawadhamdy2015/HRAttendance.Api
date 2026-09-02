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
    public MissionsController(AppDbContext db) => _db = db;

    // POST /api/missions
    // Records a مأمورية: creates the Mission entry and marks the matching
    // day on the attendance calendar as "mission", so it shows up in
    // GET /api/employees/{id}/details and GET /api/employees/{id}/missions.
    [HttpPost]
    [RequirePermission("Attendance.Manage")]
    public async Task<ActionResult<MissionDto>> Create(CreateMissionRequest request)
    {
        var employeeExists = await _db.Employees.AnyAsync(e => e.Id == request.EmployeeId);
        if (!employeeExists) return NotFound("Employee not found.");

        var mission = new Mission
        {
            EmployeeId = request.EmployeeId,
            Date = request.Date,
            Reason = request.Reason,
            Location = request.Location
        };
        _db.Missions.Add(mission);

        var record = await AttendanceHelper.GetOrCreateAsync(_db, request.EmployeeId, request.Date);
        record.Status = DayStatus.Mission;

        await _db.SaveChangesAsync();

        return Ok(new MissionDto
        {
            Id = mission.Id,
            Date = mission.Date,
            Reason = mission.Reason,
            Location = mission.Location
        });
    }
}
