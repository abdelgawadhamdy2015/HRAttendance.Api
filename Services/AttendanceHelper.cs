using HRAttendance.Api.Data;
using HRAttendance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HRAttendance.Api.Services;

public static class AttendanceHelper
{
    // Finds the AttendanceRecord for this employee/date, or creates (and stages,
    // via db.Add) a new one if none exists yet. The unique index on
    // (EmployeeId, Date) means there is always at most one record per day.
    // Caller is responsible for calling SaveChangesAsync().
    public static async Task<AttendanceRecord> GetOrCreateAsync(AppDbContext db, int employeeId, DateOnly date)
    {
        var record = await db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == employeeId && r.Date == date);

        if (record is null)
        {
            record = new AttendanceRecord { EmployeeId = employeeId, Date = date };
            db.AttendanceRecords.Add(record);
        }

        return record;
    }

    // Matches the string values already used by EmployeesController's calendar DTOs.
    public static DayStatus? StringToStatus(string status) => status switch
    {
        "present" => DayStatus.Present,
        "annualLeave" => DayStatus.AnnualLeave,
        "casualLeave" => DayStatus.CasualLeave,
        "sickLeave" => DayStatus.SickLeave,
        "permission" => DayStatus.Permission,
        "cutOff" => DayStatus.CutOff,
        "mission" => DayStatus.Mission,
        "none" => DayStatus.None,
        _ => null
    };
}
