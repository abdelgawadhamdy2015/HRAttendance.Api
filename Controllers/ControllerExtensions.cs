using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace HRAttendance.Api.Controllers;

public static class ControllerExtensions
{
    public static int? GetCurrentUserId(this ControllerBase controller)
    {
        var claim = controller.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }
}
