using HRAttendance.Api.Authorization;
using HRAttendance.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRAttendance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UsersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Permissions.View")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await db.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.FullName,
                u.IsActive,
                u.EmployeeId,
                permissions = u.UserPermissions.Select(up => up.Permission.Name).OrderBy(n => n).ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPut("{userId:int}/employee/{employeeId:int}")]
    [RequirePermission("Permissions.Manage")]
    public async Task<IActionResult> LinkEmployee(int userId, int employeeId, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) return NotFound("User not found.");

        var employeeExists = await db.Employees.AnyAsync(e => e.Id == employeeId, cancellationToken);
        if (!employeeExists) return NotFound("Employee not found.");

        user.EmployeeId = employeeId;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { userId, employeeId });
    }

    [HttpDelete("{userId:int}/employee")]
    [RequirePermission("Permissions.Manage")]
    public async Task<IActionResult> UnlinkEmployee(int userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) return NotFound("User not found.");

        user.EmployeeId = null;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
