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
                permissions = u.UserPermissions.Select(up => up.Permission.Name).OrderBy(n => n).ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }
}
