using HRAttendance.Api.Models;

namespace HRAttendance.Api.Dtos;

public sealed class AttendanceReportRequest
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public int? EmployeeId { get; set; }
    public string? Department { get; set; }
    public DayStatus? Status { get; set; }
    public bool? LateOnly { get; set; }
}

public sealed class AttendanceReportResponse
{
    public DateOnly FromDate { get; init; }
    public DateOnly ToDate { get; init; }
    public int EmployeeCount { get; init; }
    public int WorkingDays { get; init; }
    public int TotalRecords { get; init; }
    public int PresentDays { get; init; }
    public int AbsentDays { get; init; }
    public int LateDays { get; init; }
    public int TotalLateMinutes { get; init; }
    public int TotalWorkedMinutes { get; init; }
    public List<EmployeeAttendanceReportDto> Employees { get; init; } = [];
}

public sealed class EmployeeAttendanceReportDto
{
    public int EmployeeId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string JobTitle { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public int PresentDays { get; init; }
    public int AbsentDays { get; init; }
    public int LateDays { get; init; }
    public int TotalLateMinutes { get; init; }
    public int TotalWorkedMinutes { get; init; }
    public List<AttendanceDayDto> Days { get; init; } = [];
}

public sealed class AttendanceDayDto
{
    public DateOnly Date { get; init; }
    public DayStatus Status { get; init; }
    public TimeOnly? CheckIn { get; init; }
    public TimeOnly? CheckOut { get; init; }
    public int LateMinutes { get; init; }
    public int WorkedMinutes { get; init; }
}

public sealed class AttendanceActionDto
{
    public int EmployeeId { get; init; }
    public string EmployeeCode { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public DateOnly Date { get; init; }
    public string ActionType { get; init; } = string.Empty;
    public string? Time { get; init; }
    public string? Details { get; init; }
}
