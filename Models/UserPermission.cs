namespace HRAttendance.Api.Models;

// Join entity for the many-to-many relationship between User and Permission.
public class UserPermission
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}
