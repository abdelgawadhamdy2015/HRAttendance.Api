using HRAttendance.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace HRAttendance.Api.Data;

public static class SeedData
{
    public static void Seed(AppDbContext db, IConfiguration configuration)
    {
        SeedPermissions(db);
        SeedAdminFromConfiguration(db, configuration);
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

    private static void SeedAdminFromConfiguration(AppDbContext db, IConfiguration configuration)
    {
        if (db.Users.Any()) return;

        var username = configuration["BootstrapAdmin:Username"];
        var password = configuration["BootstrapAdmin:Password"];
        var email = configuration["BootstrapAdmin:Email"];
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(email)) return;

        var admin = new User { Username = username.Trim(), Email = email.Trim(), FullName = "System Administrator", IsActive = true, CreatedAt = DateTime.UtcNow };
        admin.PasswordHash = new PasswordHasher<User>().HashPassword(admin, password);
        db.Users.Add(admin);
        db.SaveChanges();

        foreach (var permissionId in db.Permissions.Select(p => p.Id).ToList())
            db.UserPermissions.Add(new UserPermission { UserId = admin.Id, PermissionId = permissionId });
        db.SaveChanges();
    }
}
