using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.Common;

namespace Yapplr.Api.Authorization;

public class RoleAuthorizationHandler : AuthorizationHandler<RoleRequirement>
{
    private readonly ICachingService _cachingService;
    private readonly IUserService _userService;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public RoleAuthorizationHandler(ICachingService cachingService, IUserService userService, IServiceScopeFactory serviceScopeFactory)
    {
        _cachingService = cachingService;
        _userService = userService;
        _serviceScopeFactory = serviceScopeFactory;
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
            var user = await _cachingService.GetUserByIdAsync(userId, _serviceScopeFactory);
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
