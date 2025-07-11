using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Yapplr.Api.Models;
using Yapplr.Api.Services;

namespace Yapplr.Api.Authorization;

public class RoleAuthorizationHandler : AuthorizationHandler<RoleRequirement>
{
    private readonly IUserCacheService _userCache;
    private readonly IUserService _userService;

    public RoleAuthorizationHandler(IUserCacheService userCache, IUserService userService)
    {
        _userCache = userCache;
        _userService = userService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        // Check if user is authenticated
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            context.Fail();
            return;
        }

        // Get current user ID
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            context.Fail();
            return;
        }

        try
        {
            // Get user entity and check role
            var user = await _userCache.GetUserByIdAsync(userId);
            if (user == null)
            {
                context.Fail();
                return;
            }

            // Check if user has sufficient role
            if (user.Role < requirement.MinimumRole)
            {
                context.Fail();
                return;
            }

            if (requirement.UserStatus.HasValue && requirement.UserStatus.Value != user.Status)
            {
                if (user.Status == UserStatus.Suspended && user.SuspendedUntil.HasValue && user.SuspendedUntil.Value <= DateTime.UtcNow)
                {
                    // Auto-unsuspend user
                    await _userService.UnsuspendUserAsync(userId);
                    // Continue with authorization since user is now unsuspended
                }
                else if (user.Status == UserStatus.Suspended || user.Status == UserStatus.Banned)
                {
                    context.Fail();
                    return;
                }
            }
            
            // If we get here, the user meets all requirements
            context.Succeed(requirement);
        }
        catch
        {
            context.Fail();
        }
    }
}
