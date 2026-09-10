using HRAttendance.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace HRAttendance.Api.Data;

public static class SeedData
{
    public static void Seed(AppDbContext db)
    {
        SeedPermissions(db);
        SeedAdminFromEnvironment(db);
    }

    private static void SeedPermissions(AppDbContext db)
    {
        var definitions = new Dictionary<string, string>
        {
            ["Dashboard.View"] = "View dashboard statistics",
            ["Notifications.View"] = "View application notifications",
            ["AuditLogs.View"] = "View audit logs",
            ["Employees.View"] = "View employee records",
            ["Employees.Manage"] = "Create and manage employee records",
            ["Attendance.View"] = "View attendance records and reports",
            ["Attendance.Manage"] = "Create and edit attendance records",
            ["Reports.View"] = "View and export attendance reports",
            ["Permissions.View"] = "View users and permissions",
            ["Permissions.Manage"] = "Create, assign, and revoke permissions",
        };

        foreach (var item in definitions)
        {
            if (db.Permissions.Any(p => p.Name == item.Key)) continue;
            db.Permissions.Add(new Permission { Name = item.Key, Description = item.Value });
        }
        db.SaveChanges();
    }

    private static void SeedAdminFromEnvironment(AppDbContext db)
    {
        if (db.Users.Any()) return;

        var username = Environment.GetEnvironmentVariable("HR_ADMIN_USERNAME");
        var password = Environment.GetEnvironmentVariable("HR_ADMIN_PASSWORD");
        var email = Environment.GetEnvironmentVariable("HR_ADMIN_EMAIL");
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(email)) return;

        var admin = new User { Username = username.Trim(), Email = email.Trim(), FullName = "System Administrator", IsActive = true, CreatedAt = DateTime.UtcNow };
        admin.PasswordHash = new PasswordHasher<User>().HashPassword(admin, password);
        db.Users.Add(admin);
        db.SaveChanges();

        var permissionIds = db.Permissions.Select(p => p.Id).ToList();
        foreach (var permissionId in permissionIds)
            db.UserPermissions.Add(new UserPermission { UserId = admin.Id, PermissionId = permissionId });
        db.SaveChanges();
    }
}
