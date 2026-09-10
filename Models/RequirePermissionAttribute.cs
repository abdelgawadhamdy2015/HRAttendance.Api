using System.Security.Claims;
using HRAttendance.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace HRAttendance.Api.Authorization;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _permission;
    public RequirePermissionAttribute(string permission) => _permission = permission;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity is null || !user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(claim, out var userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var hasPermission = await db.UserPermissions
            .AsNoTracking()
            .AnyAsync(up => up.UserId == userId && up.Permission.Name == _permission, context.HttpContext.RequestAborted);

        if (!hasPermission)
        {
            context.Result = new ObjectResult(new { message = $"Missing required permission: {_permission}" })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
