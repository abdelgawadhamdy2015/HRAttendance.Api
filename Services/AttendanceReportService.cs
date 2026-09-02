using HRAttendance.Api.Data;
using HRAttendance.Api.Dtos;
using HRAttendance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HRAttendance.Api.Services;

public interface IAttendanceReportService
{
    Task<AttendanceReportResponse> GetReportAsync(AttendanceReportRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceActionDto>> GetActionsAsync(AttendanceReportRequest request, CancellationToken cancellationToken = default);
}

public sealed class AttendanceReportService : IAttendanceReportService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;

    public AttendanceReportService(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<AttendanceReportResponse> GetReportAsync(
        AttendanceReportRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var employeesQuery = _db.Employees.AsNoTracking();
        if (request.EmployeeId.HasValue)
            employeesQuery = employeesQuery.Where(e => e.Id == request.EmployeeId.Value);
        if (!string.IsNullOrWhiteSpace(request.Department))
            employeesQuery = employeesQuery.Where(e => e.Department == request.Department);

        var employees = await employeesQuery
            .OrderBy(e => e.FullName)
            .ToListAsync(cancellationToken);

        var employeeIds = employees.Select(e => e.Id).ToList();

        var records = await _db.AttendanceRecords
            .AsNoTracking()
            .Where(a => employeeIds.Contains(a.EmployeeId)
                        && a.Date >= request.FromDate
                        && a.Date <= request.ToDate)
            .OrderBy(a => a.Date)
            .ToListAsync(cancellationToken);

        if (request.Status.HasValue)
            records = records.Where(r => r.Status == request.Status.Value).ToList();

        if (request.LateOnly == true)
            records = records.Where(r => r.LateMinutes > 0).ToList();

        var recordsByEmployee = records.GroupBy(r => r.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Date).ToList());

        var employeeReports = employees.Select(employee =>
        {
            recordsByEmployee.TryGetValue(employee.Id, out var employeeRecords);
            employeeRecords ??= [];

            var days = employeeRecords.Select(MapDay).ToList();

            return new EmployeeAttendanceReportDto
            {
                EmployeeId = employee.Id,
                Code = employee.Code,
                FullName = employee.FullName,
                JobTitle = employee.JobTitle,
                Department = employee.Department,
                PresentDays = employeeRecords.Count(x => x.Status == DayStatus.Present),
                AbsentDays = employeeRecords.Count(x => x.Status == DayStatus.CutOff),
                LateDays = employeeRecords.Count(x => x.LateMinutes > 0),
                TotalLateMinutes = employeeRecords.Sum(x => x.LateMinutes),
                TotalWorkedMinutes = employeeRecords.Sum(GetWorkedMinutes),
                Days = days
            };
        }).ToList();

        return new AttendanceReportResponse
        {
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            EmployeeCount = employees.Count,
            WorkingDays = CountCalendarDays(request.FromDate, request.ToDate),
            TotalRecords = records.Count,
            PresentDays = records.Count(x => x.Status == DayStatus.Present),
            AbsentDays = records.Count(x => x.Status == DayStatus.CutOff),
            LateDays = records.Count(x => x.LateMinutes > 0),
            TotalLateMinutes = records.Sum(x => x.LateMinutes),
            TotalWorkedMinutes = records.Sum(GetWorkedMinutes),
            Employees = employeeReports
        };
    }

    public async Task<IReadOnlyList<AttendanceActionDto>> GetActionsAsync(
        AttendanceReportRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var employeesQuery = _db.Employees.AsNoTracking();
        if (request.EmployeeId.HasValue)
            employeesQuery = employeesQuery.Where(e => e.Id == request.EmployeeId.Value);
        if (!string.IsNullOrWhiteSpace(request.Department))
            employeesQuery = employeesQuery.Where(e => e.Department == request.Department);

        var employees = await employeesQuery.ToListAsync(cancellationToken);
        var employeeIds = employees.Select(e => e.Id).ToList();
        var employeeMap = employees.ToDictionary(e => e.Id);

        var records = await _db.AttendanceRecords
            .AsNoTracking()
            .Where(a => employeeIds.Contains(a.EmployeeId)
                        && a.Date >= request.FromDate
                        && a.Date <= request.ToDate)
            .OrderBy(a => a.Date)
            .ToListAsync(cancellationToken);

        var missions = await _db.Missions
            .AsNoTracking()
            .Where(m => employeeIds.Contains(m.EmployeeId)
                        && m.Date >= request.FromDate
                        && m.Date <= request.ToDate)
            .ToListAsync(cancellationToken);

        var permissions = await _db.PermissionRequests
            .AsNoTracking()
            .Where(p => employeeIds.Contains(p.EmployeeId)
                        && p.Date >= request.FromDate
                        && p.Date <= request.ToDate)
            .ToListAsync(cancellationToken);

        var actions = new List<AttendanceActionDto>();

        foreach (var record in records)
        {
            if (!employeeMap.TryGetValue(record.EmployeeId, out var employee))
                continue;

            if (record.CheckIn.HasValue)
            {
                actions.Add(CreateAction(employee, record.Date, "CheckIn", record.CheckIn.Value.ToString("HH:mm"),
                    record.LateMinutes > 0 ? $"Late by {record.LateMinutes} minute(s)" : null));
            }

            if (record.CheckOut.HasValue)
            {
                actions.Add(CreateAction(employee, record.Date, "CheckOut", record.CheckOut.Value.ToString("HH:mm"), null));
            }

            if (record.Status != DayStatus.None && record.Status != DayStatus.Present)
            {
                actions.Add(CreateAction(employee, record.Date, record.Status.ToString(), null, null));
            }
        }

        foreach (var mission in missions)
        {
            if (employeeMap.TryGetValue(mission.EmployeeId, out var employee))
                actions.Add(CreateAction(employee, mission.Date, "Mission", null,
                    $"{mission.Reason} - {mission.Location}"));
        }

        foreach (var permission in permissions)
        {
            if (employeeMap.TryGetValue(permission.EmployeeId, out var employee))
                actions.Add(CreateAction(employee, permission.Date, "Permission",
                    $"{permission.From:HH\\:mm}-{permission.To:HH\\:mm}", permission.Reason));
        }

        return actions
            .OrderBy(x => x.Date)
            .ThenBy(x => x.EmployeeName)
            .ThenBy(x => x.Time)
            .ToList();
    }

    private static AttendanceDayDto MapDay(AttendanceRecord record) => new()
    {
        Date = record.Date,
        Status = record.Status,
        CheckIn = record.CheckIn,
        CheckOut = record.CheckOut,
        LateMinutes = record.LateMinutes,
        WorkedMinutes = GetWorkedMinutes(record)
    };

    private static int GetWorkedMinutes(AttendanceRecord record)
    {
        if (!record.CheckIn.HasValue || !record.CheckOut.HasValue)
            return 0;

        var minutes = (int)(record.CheckOut.Value - record.CheckIn.Value).TotalMinutes;
        return Math.Max(0, minutes);
    }

    private static int CountCalendarDays(DateOnly from, DateOnly to)
        => to.DayNumber - from.DayNumber + 1;

    private static AttendanceActionDto CreateAction(Employee employee, DateOnly date, string type, string? time, string? details)
        => new()
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.Code,
            EmployeeName = employee.FullName,
            Date = date,
            ActionType = type,
            Time = time,
            Details = details
        };

    private static void ValidateRequest(AttendanceReportRequest request)
    {
        if (request.FromDate == default)
            throw new ArgumentException("FromDate is required.");
        if (request.ToDate == default)
            throw new ArgumentException("ToDate is required.");
        if (request.FromDate > request.ToDate)
            throw new ArgumentException("FromDate cannot be after ToDate.");
        if ((request.ToDate.DayNumber - request.FromDate.DayNumber) > 366)
            throw new ArgumentException("The report date range cannot exceed 367 days.");
    }
}
