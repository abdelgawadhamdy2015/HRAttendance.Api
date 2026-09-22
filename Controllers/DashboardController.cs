using System.Security.Claims;
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
    public async Task<ActionResult<DashboardStatsDto>> GetStats([FromQuery] DateOnly? date)
    {
        var d = date ?? DateOnly.FromDateTime(DateTime.Today);
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var permissions = await _db.UserPermissions
            .Where(up => up.UserId == userId.Value)
            .Select(up => up.Permission.Name)
            .ToHashSetAsync();

        var canDashboard = HasView(permissions, "Dashboard.View");
        if (!canDashboard)
        {
            return Ok(new DashboardStatsDto
            {
                Date = d,
                TotalEmployees = 0,
                PresentToday = 0,
                LateToday = 0,
                OnMission = 0,
                OnLeave = 0,
                AbsentToday = 0
            });
        }

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
    public async Task<ActionResult<List<NotificationDto>>> GetNotifications()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var items = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId.Value)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Message = n.Message,
                Severity = n.Severity.ToString().ToLowerInvariant()
            }).ToListAsync();
        return Ok(items);
    }

    private int? GetCurrentUserId()
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static bool HasView(HashSet<string> permissions, string permission)
    {
        if (permissions.Contains(permission)) return true;
        var baseName = permission.EndsWith(".View", StringComparison.Ordinal)
            ? permission[..^5]
            : permission;
        return permissions.Contains(baseName + ".Manage") || permissions.Contains(baseName + ".Edit");
    }
}
