using HRAttendance.Api.Models;

namespace HRAttendance.Api.Data;

public static class CaseStatisticsSeedData
{
    public static readonly string[] CaseStatisticsPermissionNames =
    {
        "Reports.Manage",
        "Reports.Finalize",
        "Reports.Reopen",
        "Statistics.Edit",
        "Statistics.View",
    };

    public static void Seed(AppDbContext db, IConfiguration configuration)
    {
        foreach (var name in CaseStatisticsPermissionNames)
        {
            if (!db.Permissions.Any(p => p.Name == name))
                db.Permissions.Add(new Permission { Name = name, Description = $"Case statistics: {name}" });
        }
        db.SaveChanges();

        var username = configuration["BootstrapAdmin:Username"];
        if (string.IsNullOrWhiteSpace(username)) return;
        var admin = db.Users.FirstOrDefault(u => u.Username == username);
        if (admin is null) return;

        var permissionIds = db.Permissions.Where(p => CaseStatisticsPermissionNames.Contains(p.Name)).Select(p => p.Id).ToList();
        foreach (var permissionId in permissionIds)
        {
            if (!db.UserPermissions.Any(up => up.UserId == admin.Id && up.PermissionId == permissionId))
                db.UserPermissions.Add(new UserPermission { UserId = admin.Id, PermissionId = permissionId });
        }
        db.SaveChanges();
    }
}
