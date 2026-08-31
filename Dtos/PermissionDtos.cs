namespace HRAttendance.Api.DTOs;

public record CreatePermissionRequest(string Name, string? Description);

public record AssignPermissionRequest(int UserId, int PermissionId);
