using System;

namespace HRAttendance.Api.Dtos;

// ---- Employees ----
public record CreateEmployeeRequest(
    string Code,
    string FullName,
    string JobTitle,
    string Department,
    string? AvatarUrl);

// ---- Attendance ----

// Date/Time are optional - if omitted, "now" is used. This lets a kiosk/mobile
// app just send { "employeeId": 1 } and get today's check-in recorded.
public record CheckInRequest(int EmployeeId, DateOnly? Date, TimeOnly? Time);

public record CheckOutRequest(int EmployeeId, DateOnly? Date, TimeOnly? Time);

// Status must be one of: present | annualLeave | casualLeave | sickLeave | cutOff | none
// (Permission and Mission have their own dedicated endpoints below because they
// also create a PermissionRequest / Mission record, not just an attendance day.)
public record MarkAttendanceRequest(int EmployeeId, DateOnly Date, string Status);

public record RecordLatenessRequest(int EmployeeId, DateOnly Date, int Minutes);

// ---- Missions (مأمورية) ----
public record CreateMissionRequest(int EmployeeId, DateOnly Date, string Reason, string Location);

// ---- Permission requests / إذن ----
// From/To are "HH:mm" strings, e.g. "11:00".
public record CreatePermissionRequestDto(int EmployeeId, DateOnly Date, string From, string To, string Reason);
