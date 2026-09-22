using System.Security.Claims;
using HRAttendance.Api.Authorization;
using HRAttendance.Api.Data;
using HRAttendance.Api.Dtos;
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
        await _audit.NotifyAsync(request.UserId, $"تم منح الصلاحية رقم {request.PermissionId} للمستخدم رقم {request.UserId}.");
        return Ok();
    }

    [HttpPost("assign-many")]
    [RequirePermission("Permissions.Manage")]
    public async Task<IActionResult> AssignMany(UpdateUserPermissionsRequestDto request)
    {
        var userExists = await _db.Users.AnyAsync(u => u.Id == request.UserId);

        if (!userExists)
            return NotFound("User not found.");

        var permissionIds = request.PermissionIds
            .Distinct()
            .ToList();

        var validPermissionIds = await _db.Permissions
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();

        if (validPermissionIds.Count != permissionIds.Count)
            return BadRequest("One or more permissions do not exist.");

        // Get current permissions
        var currentPermissions = await _db.UserPermissions
            .Where(up => up.UserId == request.UserId)
            .ToListAsync();

        // Remove old permissions
        _db.UserPermissions.RemoveRange(currentPermissions);

        // Add new permissions
        var newPermissions = validPermissionIds.Select(permissionId =>
            new UserPermission
            {
                UserId = request.UserId,
                PermissionId = permissionId
            });

        await _db.UserPermissions.AddRangeAsync(newPermissions);

        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            CurrentUserId,
            "Permission.Update",
            "UserPermission",
            request.UserId,
            null,
            new
            {
                request.UserId,
                PermissionIds = validPermissionIds
            });

        await _audit.NotifyAsync(
            $"تم تحديث صلاحيات المستخدم رقم {request.UserId}.");
        var updatedPermissions = await _db.UserPermissions
                   .Where(up => up.UserId == request.UserId)
                   .ToListAsync();
        return Ok(updatedPermissions);
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
        await _audit.NotifyAsync(request.UserId, $"تم سحب الصلاحية رقم {request.PermissionId} من المستخدم رقم {request.UserId}.", NotificationSeverity.Warning);
        return Ok();
    }

    [HttpPut("user/{userId:int}")]
    [RequirePermission("Permissions.Manage")]
    public async Task<IActionResult> UpdateUserPermissions(int userId, UpdateUserPermissionsRequest request)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == userId))
            return NotFound("User not found.");

        var permissionIds = request.PermissionIds.Distinct().ToList();
        var validPermissionIds = await _db.Permissions
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();

        if (validPermissionIds.Count != permissionIds.Count)
            return BadRequest("One or more permission IDs are invalid.");

        var existing = await _db.UserPermissions
            .Where(up => up.UserId == userId)
            .ToListAsync();

        var currentIds = existing.Select(up => up.PermissionId).ToHashSet();
        var requestedIds = validPermissionIds.ToHashSet();

        var added = requestedIds.Except(currentIds).ToList();
        var removed = currentIds.Except(requestedIds).ToList();

        if (removed.Count > 0)
            _db.UserPermissions.RemoveRange(existing.Where(up => removed.Contains(up.PermissionId)));

        foreach (var permissionId in added)
            _db.UserPermissions.Add(new UserPermission
            {
                UserId = userId,
                PermissionId = permissionId
            });

        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            CurrentUserId,
            "Permission.UpdateUser",
            "UserPermission",
            userId,
            new { PermissionIds = currentIds.OrderBy(x => x).ToList() },
            new { PermissionIds = requestedIds.OrderBy(x => x).ToList() });

        if (added.Count > 0 || removed.Count > 0)
        {
            await _audit.NotifyAsync(
                userId,
                $"تم تحديث صلاحيات المستخدم رقم {userId}.",
                removed.Count > 0 ? NotificationSeverity.Warning : NotificationSeverity.Info);
        }

        return Ok(await _db.UserPermissions
            .Where(up => up.UserId == userId)
            .Select(up => up.Permission)
            .OrderBy(p => p.Name)
            .ToListAsync());
    }

}
