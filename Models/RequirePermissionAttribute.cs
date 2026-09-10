using System.Security.Claims;
using HRAttendance.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace HRAttendance.Api.Authorization;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _permission;
    public RequirePermissionAttribute(string permission) => _permission = permission;

    public void OnAuthorization(AuthorizationFilterContext context)
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
        var hasPermission = db.UserPermissions
            .AsNoTracking()
            .Any(up => up.UserId == userId && up.Permission.Name == _permission);

        if (!hasPermission)
        {
            context.Result = new ObjectResult(new { message = $"Missing required permission: {_permission}" })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
