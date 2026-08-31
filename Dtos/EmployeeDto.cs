namespace HRAttendance.Api.Dtos;

public class EmployeeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

public class AttendanceDayDto
{
    public DateOnly Date { get; set; }
    public string Status { get; set; } = "none";
    // present | annualLeave | casualLeave | sickLeave | permission | cutOff | mission | none
}

public class EmployeeMonthDetailsDto
{
    public int EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }

    public int TotalPresentDays { get; set; }
    public int AnnualLeaveDays { get; set; }
    public int CasualLeaveDays { get; set; }
    public int SickLeaveDays { get; set; }
    public int CutOffDays { get; set; }
    public int PermissionsUsed { get; set; }
    public int PermissionsAllowed { get; set; }
    public int TotalLateMinutes { get; set; }

    public List<AttendanceDayDto> Days { get; set; } = new();
}

public class MissionDto
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

public class PermissionDto
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class LatenessDto
{
    public DateOnly Date { get; set; }
    public int Minutes { get; set; }
}
