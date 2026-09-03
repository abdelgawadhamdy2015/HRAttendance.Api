using HRAttendance.Api.Authorization;
using HRAttendance.Api.Data;
using HRAttendance.Api.Dtos;
using HRAttendance.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRAttendance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _db;
    public EmployeesController(AppDbContext db) => _db = db;

    private static string StatusToString(DayStatus s) => s switch
    {
        DayStatus.Present => "present",
        DayStatus.AnnualLeave => "annualLeave",
        DayStatus.CasualLeave => "casualLeave",
        DayStatus.SickLeave => "sickLeave",
        DayStatus.Permission => "permission",
        DayStatus.CutOff => "cutOff",
        DayStatus.Mission => "mission",
        _ => "none"
    };

    // GET /api/employees
    [HttpGet]
    public async Task<ActionResult<List<EmployeeDto>>> GetAll()
    {
        var items = await _db.Employees
            .Select(e => new EmployeeDto
            {
                Id = e.Id,
                Code = e.Code,
                FullName = e.FullName,
                JobTitle = e.JobTitle,
                Department = e.Department,
                AvatarUrl = e.AvatarUrl
            })
            .ToListAsync();
        return Ok(items);
    }

    // POST /api/employees
    // Creates a new employee record. Codes are expected to be unique
    // (e.g. "031"), matching the convention used by SeedData.
    [HttpPost]
    [RequirePermission("Employees.Manage")]
    public async Task<ActionResult<EmployeeDto>> Create(CreateEmployeeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest("Code and FullName are required.");

        if (await _db.Employees.AnyAsync(e => e.Code == request.Code))
            return Conflict("An employee with this code already exists.");

        var employee = new Employee
        {
            Code = request.Code,
            FullName = request.FullName,
            JobTitle = request.JobTitle,
            Department = request.Department,
            AvatarUrl = request.AvatarUrl
        };

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, new EmployeeDto
        {
            Id = employee.Id,
            Code = employee.Code,
            FullName = employee.FullName,
            JobTitle = employee.JobTitle,
            Department = employee.Department,
            AvatarUrl = employee.AvatarUrl
        });
    }

    // GET /api/employees/1
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> GetById(int id)
    {
        var e = await _db.Employees.FindAsync(id);
        if (e == null) return NotFound();

        return Ok(new EmployeeDto
        {
            Id = e.Id,
            Code = e.Code,
            FullName = e.FullName,
            JobTitle = e.JobTitle,
            Department = e.Department,
            AvatarUrl = e.AvatarUrl
        });
    }

    // GET /api/employees/1/details?year=2024&month=5
    [HttpGet("{id:int}/details")]
    public async Task<ActionResult<EmployeeMonthDetailsDto>> GetMonthDetails(int id, [FromQuery] int year, [FromQuery] int month)
    {
        var records = await _db.AttendanceRecords
            .Where(r => r.EmployeeId == id && r.Date.Year == year && r.Date.Month == month)
            .ToListAsync();

        var permissionsUsed = await _db.PermissionRequests
            .CountAsync(p => p.EmployeeId == id && p.Date.Year == year && p.Date.Month == month);

        var dto = new EmployeeMonthDetailsDto
        {
            EmployeeId = id,
            Year = year,
            Month = month,
            TotalPresentDays = records.Count(r => r.Status == DayStatus.Present),
            AnnualLeaveDays = records.Count(r => r.Status == DayStatus.AnnualLeave),
            CasualLeaveDays = records.Count(r => r.Status == DayStatus.CasualLeave),
            SickLeaveDays = records.Count(r => r.Status == DayStatus.SickLeave),
            CutOffDays = records.Count(r => r.Status == DayStatus.CutOff),
            PermissionsUsed = permissionsUsed,
            PermissionsAllowed = 2,
            TotalLateMinutes = records.Sum(r => r.LateMinutes),
            Days = records.Select(r => new EmployeeAttendanceDayDto { Date = r.Date, Status = StatusToString(r.Status) }).ToList()
        };

        return Ok(dto);
    }

    // GET /api/employees/1/missions?year=2024&month=5
    [HttpGet("{id:int}/missions")]
    public async Task<ActionResult<List<MissionDto>>> GetMissions(int id, [FromQuery] int year, [FromQuery] int month)
    {
        var items = await _db.Missions
            .Where(m => m.EmployeeId == id && m.Date.Year == year && m.Date.Month == month)
            .Select(m => new MissionDto { Id = m.Id, Date = m.Date, Reason = m.Reason, Location = m.Location })
            .ToListAsync();
        return Ok(items);
    }

    // GET /api/employees/1/permissions?year=2024&month=5
    [HttpGet("{id:int}/permissions")]
    public async Task<ActionResult<List<PermissionDto>>> GetPermissions(int id, [FromQuery] int year, [FromQuery] int month)
    {
        var items = await _db.PermissionRequests
            .Where(p => p.EmployeeId == id && p.Date.Year == year && p.Date.Month == month)
            .Select(p => new PermissionDto
            {
                Id = p.Id,
                Date = p.Date,
                From = p.From.ToString("HH:mm"),
                To = p.To.ToString("HH:mm"),
                Reason = p.Reason
            })
            .ToListAsync();
        return Ok(items);
    }

    // GET /api/employees/1/lateness?year=2024&month=5
    [HttpGet("{id:int}/lateness")]
    public async Task<ActionResult<List<LatenessDto>>> GetLateness(int id, [FromQuery] int year, [FromQuery] int month)
    {
        var items = await _db.AttendanceRecords
            .Where(r => r.EmployeeId == id && r.Date.Year == year && r.Date.Month == month && r.LateMinutes > 0)
            .Select(r => new LatenessDto { Date = r.Date, Minutes = r.LateMinutes })
            .ToListAsync();
        return Ok(items);
    }
}
