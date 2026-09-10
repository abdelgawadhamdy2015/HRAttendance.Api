using HRAttendance.Api.Authorization;
using HRAttendance.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRAttendance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AuditLogsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("AuditLogs.View")]
    public async Task<IActionResult> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = db.AuditLogs.AsNoTracking().OrderByDescending(x => x.Timestamp);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return Ok(new { items, page, pageSize, total });
    }
}
