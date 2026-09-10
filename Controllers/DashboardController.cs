using HRAttendance.Api.Authorization;
using HRAttendance.Api.Data;
using HRAttendance.Api.Dtos;
using HRAttendance.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRAttendance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    public DashboardController(AppDbContext db) => _db = db;

    [HttpGet("stats")]
    [RequirePermission("Dashboard.View")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats([FromQuery] DateOnly? date)
    {
        var d = date ?? DateOnly.FromDateTime(DateTime.Today);
        var totalEmployees = await _db.Employees.CountAsync();
        var records = await _db.AttendanceRecords.Where(r => r.Date == d).ToListAsync();
        return Ok(new DashboardStatsDto
        {
            Date = d,
            TotalEmployees = totalEmployees,
            PresentToday = records.Count(r => r.Status == DayStatus.Present),
            LateToday = records.Count(r => r.Status == DayStatus.Present && r.LateMinutes > 0),
            OnMission = records.Count(r => r.Status == DayStatus.Mission),
            OnLeave = records.Count(r => r.Status is DayStatus.AnnualLeave or DayStatus.CasualLeave or DayStatus.SickLeave),
            AbsentToday = Math.Max(0, totalEmployees - records.Count)
        });
    }

    [HttpGet("notifications")]
    [RequirePermission("Notifications.View")]
    public async Task<ActionResult<List<NotificationDto>>> GetNotifications()
    {
        var items = await _db.Notifications.OrderByDescending(n => n.CreatedAt).Select(n => new NotificationDto
        {
            Id = n.Id,
            Message = n.Message,
            Severity = n.Severity.ToString().ToLowerInvariant()
        }).ToListAsync();
        return Ok(items);
    }
}
