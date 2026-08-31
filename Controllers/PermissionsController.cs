using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HRAttendance.Api.Authorization;
using HRAttendance.Api.Data;
using HRAttendance.Api.DTOs;
using HRAttendance.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRAttendance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PermissionsController(AppDbContext db)
    {
        _db = db;
    }

    // Anyone logged in can see the catalog of permissions that exist.
    [HttpGet]
    public async Task<ActionResult<List<Permission>>> GetAll()
    {
        return await _db.Permissions.OrderBy(p => p.Name).ToListAsync();
    }

    // Only users holding "Permissions.Manage" can create new permission types.
    [HttpPost]
    [RequirePermission("Permissions.Manage")]
    public async Task<ActionResult<Permission>> Create(CreatePermissionRequest request)
    {
        if (await _db.Permissions.AnyAsync(p => p.Name == request.Name))
            return Conflict("A permission with this name already exists.");

        var permission = new Permission { Name = request.Name, Description = request.Description };
        _db.Permissions.Add(permission);
        await _db.SaveChangesAsync();
        return Ok(permission);
    }

    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<List<Permission>>> GetForUser(int userId)
    {
        var permissions = await _db.UserPermissions
            .Where(up => up.UserId == userId)
            .Select(up => up.Permission)
            .ToListAsync();
        return Ok(permissions);
    }

    [HttpPost("assign")]
    [RequirePermission("Permissions.Manage")]
    public async Task<IActionResult> Assign(AssignPermissionRequest request)
    {
        var userExists = await _db.Users.AnyAsync(u => u.Id == request.UserId);
        var permissionExists = await _db.Permissions.AnyAsync(p => p.Id == request.PermissionId);
        if (!userExists || !permissionExists) return NotFound();

        var already = await _db.UserPermissions.AnyAsync(up =>
            up.UserId == request.UserId && up.PermissionId == request.PermissionId);
        if (already) return Ok();

        _db.UserPermissions.Add(new UserPermission
        {
            UserId = request.UserId,
            PermissionId = request.PermissionId
        });
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("revoke")]
    [RequirePermission("Permissions.Manage")]
    public async Task<IActionResult> Revoke(AssignPermissionRequest request)
    {
        var link = await _db.UserPermissions.FirstOrDefaultAsync(up =>
            up.UserId == request.UserId && up.PermissionId == request.PermissionId);
        if (link is null) return NotFound();

        _db.UserPermissions.Remove(link);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
