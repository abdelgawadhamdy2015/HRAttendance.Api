using System.Security.Claims;
using HRAttendance.Api.Authorization;
using HRAttendance.Api.Data;
using HRAttendance.Api.DTOs;
using HRAttendance.Api.Models;
using HRAttendance.Api.Services;
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
    private readonly IAuditService _audit;

    public PermissionsController(AppDbContext db, IAuditService audit) { _db = db; _audit = audit; }
    private int? CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet]
    [RequirePermission("Permissions.View")]
    public async Task<ActionResult<List<Permission>>> GetAll() => await _db.Permissions.OrderBy(p => p.Name).ToListAsync();

    [HttpPost]
    [RequirePermission("Permissions.Manage")]
    public async Task<ActionResult<Permission>> Create(CreatePermissionRequest request)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest("Permission name is required.");
        if (await _db.Permissions.AnyAsync(p => p.Name == name)) return Conflict("A permission with this name already exists.");
        var permission = new Permission { Name = name, Description = request.Description?.Trim() };
        _db.Permissions.Add(permission);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, "Permission.Create", "Permission", permission.Id, null, new { permission.Id, permission.Name, permission.Description });
        return Ok(permission);
    }

    [HttpGet("user/{userId:int}")]
    [RequirePermission("Permissions.View")]
    public async Task<ActionResult<List<Permission>>> GetForUser(int userId)
    {
        var exists = await _db.Users.AnyAsync(u => u.Id == userId);
        if (!exists) return NotFound("User not found.");
        return Ok(await _db.UserPermissions.Where(up => up.UserId == userId).Select(up => up.Permission).OrderBy(p => p.Name).ToListAsync());
    }

    [HttpPost("assign")]
    [RequirePermission("Permissions.Manage")]
    public async Task<IActionResult> Assign(AssignPermissionRequest request)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == request.UserId) || !await _db.Permissions.AnyAsync(p => p.Id == request.PermissionId)) return NotFound();
        var already = await _db.UserPermissions.AnyAsync(up => up.UserId == request.UserId && up.PermissionId == request.PermissionId);
        if (already) return Ok();
        _db.UserPermissions.Add(new UserPermission { UserId = request.UserId, PermissionId = request.PermissionId });
        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, "Permission.Assign", "UserPermission", request.PermissionId, null, new { request.UserId, request.PermissionId });
        await _audit.NotifyAsync($"تم منح الصلاحية رقم {request.PermissionId} للمستخدم رقم {request.UserId}.");
        return Ok();
    }

    [HttpPost("revoke")]
    [RequirePermission("Permissions.Manage")]
    public async Task<IActionResult> Revoke(AssignPermissionRequest request)
    {
        var link = await _db.UserPermissions.FirstOrDefaultAsync(up => up.UserId == request.UserId && up.PermissionId == request.PermissionId);
        if (link is null) return NotFound();
        _db.UserPermissions.Remove(link);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, "Permission.Revoke", "UserPermission", request.PermissionId, new { request.UserId, request.PermissionId }, null);
        await _audit.NotifyAsync($"تم سحب الصلاحية رقم {request.PermissionId} من المستخدم رقم {request.UserId}.", NotificationSeverity.Warning);
        return Ok();
    }
}
