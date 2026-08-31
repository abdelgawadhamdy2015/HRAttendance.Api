using HRAttendance.Api.Data;
using HRAttendance.Api.Dtos;
using HRAttendance.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRAttendance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    public DashboardController(AppDbContext db) => _db = db;

    // GET /api/dashboard/stats?date=2024-05-30
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats([FromQuery] DateOnly? date)
    {
        var d = date ?? DateOnly.FromDateTime(DateTime.Today);

        var totalEmployees = await _db.Employees.CountAsync();
        var records = await _db.AttendanceRecords.Where(r => r.Date == d).ToListAsync();

        var present = records.Count(r => r.Status == DayStatus.Present);
        var late = records.Count(r => r.Status == DayStatus.Present && r.LateMinutes > 0);
        var mission = records.Count(r => r.Status == DayStatus.Mission);
        var onLeave = records.Count(r => r.Status is DayStatus.AnnualLeave or DayStatus.CasualLeave or DayStatus.SickLeave);
        var absent = Math.Max(0, totalEmployees - records.Count);

        return Ok(new DashboardStatsDto
        {
            Date = d,
            TotalEmployees = totalEmployees,
            PresentToday = present,
            LateToday = late,
            OnMission = mission,
            OnLeave = onLeave,
            AbsentToday = absent
        });
    }

    // GET /api/dashboard/notifications
    [HttpGet("notifications")]
    public async Task<ActionResult<List<NotificationDto>>> GetNotifications()
    {
        var items = await _db.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Message = n.Message,
                Severity = n.Severity.ToString().ToLowerInvariant()
            })
            .ToListAsync();

        return Ok(items);
    }
}
