using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using Yapplr.Api.Models;
using Yapplr.Api.Services;

namespace Yapplr.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly UserRole _minimumRole;

    public RequireRoleAttribute(UserRole minimumRole)
    {
        _minimumRole = minimumRole;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Get user service
        var userService = context.HttpContext.RequestServices.GetService<IUserService>();
        if (userService == null)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        // Get current user ID
        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        try
        {
            // Get user entity and check role
            var user = await userService.GetUserEntityByIdAsync(userId);
            if (user == null)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Check if user has sufficient role
            if (user.Role < _minimumRole)
            {
                context.Result = new ForbidResult();
                return;
            }

            // Check if user is suspended or banned
            if (user.Status == UserStatus.Suspended || user.Status == UserStatus.Banned)
            {
                context.Result = new ForbidResult();
                return;
            }

            // If suspended temporarily, check if suspension has expired
            if (user.Status == UserStatus.Suspended && user.SuspendedUntil.HasValue && user.SuspendedUntil.Value <= DateTime.UtcNow)
            {
                // Auto-unsuspend user
                await userService.UnsuspendUserAsync(userId);
            }
        }
        catch
        {
            context.Result = new StatusCodeResult(500);
        }
    }
}

// Convenience attributes for specific roles
public class RequireModeratorAttribute : RequireRoleAttribute
{
    public RequireModeratorAttribute() : base(UserRole.Moderator) { }
}

public class RequireAdminAttribute : RequireRoleAttribute
{
    public RequireAdminAttribute() : base(UserRole.Admin) { }
}
