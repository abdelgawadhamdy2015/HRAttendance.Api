using HRAttendance.Api.Authorization;
using HRAttendance.Api.Data;
using HRAttendance.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRAttendance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class NotificationsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Notifications.View")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var items = await db.Notifications.AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Message = n.Message,
                Severity = n.Severity.ToString().ToLowerInvariant()
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }
}
