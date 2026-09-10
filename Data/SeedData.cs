using HRAttendance.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace HRAttendance.Api.Data;

public static class SeedData
{
    public static void Seed(AppDbContext db)
    {
        // The application must use real database records. Do not seed demo
        // employees, attendance records, missions, permission requests, or
        // notifications.
        SeedPermissions(db);
        SeedAdmin(db);
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

    private static void SeedAdmin(AppDbContext db)
    {
        var admin = db.Users.FirstOrDefault(u => u.Username == "admin");
        if (admin is null)
        {
            admin = new User
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
        }

        var permissionIds = db.Permissions.Select(p => p.Id).ToList();
        var existingIds = db.UserPermissions.Where(up => up.UserId == admin.Id).Select(up => up.PermissionId).ToHashSet();
        foreach (var permissionId in permissionIds)
        {
            if (existingIds.Contains(permissionId)) continue;
            db.UserPermissions.Add(new UserPermission { UserId = admin.Id, PermissionId = permissionId });
        }
        db.SaveChanges();
    }
}
