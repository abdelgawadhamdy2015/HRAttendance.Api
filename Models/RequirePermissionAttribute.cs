using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HRAttendance.Api.Authorization;

// Put this on a controller or action to require the logged-in user to hold a specific
// permission claim, e.g.  [RequirePermission("Attendance.Approve")]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _permission;

    public RequirePermissionAttribute(string permission)
    {
        _permission = permission;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user.Identity is null || !user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var hasPermission = user.Claims.Any(c => c.Type == "permission" && c.Value == _permission);
        if (!hasPermission)
        {
            context.Result = new ObjectResult(new { message = $"Missing required permission: {_permission}" })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
