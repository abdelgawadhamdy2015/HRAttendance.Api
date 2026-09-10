using HRAttendance.Api.Models;

namespace HRAttendance.Api.Data;

/// <summary>
/// Seeds the RBAC permissions used by the case-statistics domain and grants
/// them all to the "admin" user created by SeedData.Seed(). Real production
/// employee/report data should come from POST /api/employees and the Excel
/// import endpoint (phase 3) - not from source code.
/// </summary>
public static class CaseStatisticsSeedData
{
    public static readonly string[] CaseStatisticsPermissionNames =
    {
        "Reports.Manage",   // create/edit/delete draft reports, add/remove employees
        "Reports.Finalize", // lock a report
        "Reports.Reopen",   // admin-only: unlock a finalized report
        "Statistics.Edit",  // enter/edit an employee's monthly figures
        "Statistics.View",  // read-only access (Viewer role)
    };

    public static void Seed(AppDbContext db)
    {
        foreach (var name in CaseStatisticsPermissionNames)
        {
            if (!db.Permissions.Any(p => p.Name == name))
                db.Permissions.Add(new Permission { Name = name, Description = $"Case statistics: {name}" });
        }
        db.SaveChanges();

        var admin = db.Users.FirstOrDefault(u => u.Username == "admin");
        if (admin is null) return;

        var permissionIds = db.Permissions
            .Where(p => CaseStatisticsPermissionNames.Contains(p.Name))
            .Select(p => p.Id)
            .ToList();

        foreach (var permissionId in permissionIds)
        {
            var already = db.UserPermissions.Any(up => up.UserId == admin.Id && up.PermissionId == permissionId);
            if (!already)
                db.UserPermissions.Add(new UserPermission { UserId = admin.Id, PermissionId = permissionId });
        }
        db.SaveChanges();
    }
}
