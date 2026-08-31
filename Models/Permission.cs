using System.Collections.Generic;

namespace HRAttendance.Api.Models;

// A single named capability, e.g. "Attendance.Create", "Employees.View", "Permissions.Manage".
// Assign these to Users through UserPermission to control what each user can do or see.
public class Permission
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
