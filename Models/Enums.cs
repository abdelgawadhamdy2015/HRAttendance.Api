namespace HRAttendance.Api.Models;

// Matches the legend in the calendar screen:
// حاضر / إجازة اعتيادية / إجازة عارضة / إجازة مرضية / إذن / انقطاع / مأمورية
public enum DayStatus
{
    None = 0,
    Present = 1,        // حاضر
    AnnualLeave = 2,     // إجازة اعتيادية
    CasualLeave = 3,     // إجازة عارضة
    SickLeave = 4,       // إجازة مرضية
    Permission = 5,      // إذن
    CutOff = 6,          // انقطاع
    Mission = 7          // مأمورية
}

public enum NotificationSeverity
{
    Info = 0,
    Warning = 1,
    Danger = 2
}
