using System;
using System.Collections.Generic;

namespace HRAttendance.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Optional link to the employee represented by this login.
    // A user can still log in and use non-permission features when this is null.
    public int? EmployeeId { get; set; }

    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
