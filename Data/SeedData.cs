using HRAttendance.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace HRAttendance.Api.Data;

public static class SeedData
{
    public static void Seed(AppDbContext db)
    {
        if (db.Employees.Any()) return;

        var employees = new List<Employee>
        {
            new() { Code = "001", FullName = "أحمد محمد أحمد", JobTitle = "موظف إداري - الدرجة الثالثة", Department = "إدارة الشؤون الإدارية" },
            new() { Code = "002", FullName = "سارة علي حسن", JobTitle = "محاسبة - الدرجة الثانية", Department = "الإدارة المالية" },
        };

        // pad up to 30 employees for the dashboard "total employees" count
        for (int i = 3; i <= 30; i++)
        {
            employees.Add(new Employee
            {
                Code = i.ToString("000"),
                FullName = $"موظف تجريبي {i}",
                JobTitle = "موظف إداري",
                Department = "إدارة الشؤون الإدارية"
            });
        }

        db.Employees.AddRange(employees);
        db.SaveChanges();

        var ahmed = employees.First(e => e.Code == "001");

        // Recreate the May 2024 calendar exactly as shown in the screenshot:
        // 12 (Sun)  -> cutOff (انقطاع)
        // 12 (Tue)  -> mission (dot, مأمورية)
        // 21 (Sat)  -> annualLeave (إجازة)
        // 16 (Mon)  -> permission (إذن)
        // 25 (Sat)  -> sick (مرضي)
        // 25 (Tue)  -> cutOff (انقطاع)
        // 30 (Thu, circled = today)
        var records = new List<AttendanceRecord>
        {
            new() { EmployeeId = ahmed.Id, Date = new DateOnly(2024, 5, 12), Status = DayStatus.CutOff },
            new() { EmployeeId = ahmed.Id, Date = new DateOnly(2024, 5, 14), Status = DayStatus.Mission },
            new() { EmployeeId = ahmed.Id, Date = new DateOnly(2024, 5, 18), Status = DayStatus.AnnualLeave },
            new() { EmployeeId = ahmed.Id, Date = new DateOnly(2024, 5, 20), Status = DayStatus.Permission },
            new() { EmployeeId = ahmed.Id, Date = new DateOnly(2024, 5, 22), Status = DayStatus.SickLeave },
            new() { EmployeeId = ahmed.Id, Date = new DateOnly(2024, 5, 25), Status = DayStatus.CutOff },
            new() { EmployeeId = ahmed.Id, Date = new DateOnly(2024, 5, 30), Status = DayStatus.Present, LateMinutes = 0 },
        };

        // Fill remaining working days in May 2024 as "present" (22 present days total per screenshot)
        var usedDates = records.Select(r => r.Date).ToHashSet();
        var presentCount = 0;
        for (int day = 1; day <= 31 && presentCount < 22; day++)
        {
            var date = new DateOnly(2024, 5, day);
            if (usedDates.Contains(date)) continue;
            if (date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday) continue; // weekend
            records.Add(new AttendanceRecord
            {
                EmployeeId = ahmed.Id,
                Date = date,
                Status = DayStatus.Present,
                LateMinutes = day % 7 == 0 ? 10 : 0
            });
            presentCount++;
        }

        db.AttendanceRecords.AddRange(records);

        db.Missions.Add(new Mission
        {
            EmployeeId = ahmed.Id,
            Date = new DateOnly(2024, 5, 14),
            Reason = "اجتماع خارجي",
            Location = "الإدارة المركزية"
        });

        db.PermissionRequests.Add(new PermissionRequest
        {
            EmployeeId = ahmed.Id,
            Date = new DateOnly(2024, 5, 20),
            From = new TimeOnly(11, 0),
            To = new TimeOnly(13, 0),
            Reason = "ظرف طارئ"
        });

        db.Notifications.AddRange(
            new AppNotification { Message = "الموظف أحمد محمد تجاوز 60 دقيقة تأخير هذا الشهر", Severity = NotificationSeverity.Warning, CreatedAt = DateTime.UtcNow },
            new AppNotification { Message = "الموظف سارة علي استنفدت أذون الشهر", Severity = NotificationSeverity.Danger, CreatedAt = DateTime.UtcNow },
            new AppNotification { Message = "يوجد 2 طلب إجازة قيد الموافقة", Severity = NotificationSeverity.Info, CreatedAt = DateTime.UtcNow }
        );

        if (!db.Permissions.Any())
        {
            var permissions = new[]
            {
        new Permission { Name = "Permissions.Manage", Description = "Create permissions and assign them to users" },
        new Permission { Name = "Employees.View", Description = "View employee records" },
        new Permission { Name = "Employees.Manage", Description = "Create/edit/delete employee records" },
        new Permission { Name = "Attendance.View", Description = "View attendance records" },
        new Permission { Name = "Attendance.Manage", Description = "Create/edit attendance records" },
    };
            db.Permissions.AddRange(permissions);
            db.SaveChanges();
        }

        if (!db.Users.Any())
        {
            var admin = new User
            {
                Username = "admin",
                Email = "admin@example.com",
                FullName = "System Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            admin.PasswordHash = new PasswordHasher<User>().HashPassword(admin, "Admin@123");
            db.Users.Add(admin);
            db.SaveChanges();

            var manageAllPermission = db.Permissions.First(p => p.Name == "Permissions.Manage");
            db.UserPermissions.Add(new UserPermission { UserId = admin.Id, PermissionId = manageAllPermission.Id });
            db.SaveChanges();
        }
        db.SaveChanges();
    }
}
