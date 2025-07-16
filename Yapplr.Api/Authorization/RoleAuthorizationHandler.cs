using Microsoft.AspNetCore.Authorization;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.Common;
using Yapplr.Api.Extensions;

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
        if (!context.User.IsAuthenticated())
        {
            context.Fail();
            return;
        }

        // Check role using JWT claims (no database query needed)
        if (!context.User.HasRoleOrHigher(requirement.MinimumRole))
        {
            context.Fail();
            return;
        }

        // If no status requirement, we're done (role check passed)
        if (!requirement.UserStatus.HasValue)
        {
            context.Succeed(requirement);
            return;
        }

        // Status check requires database query for real-time data
        var userId = context.User.GetUserIdOrNull();
        if (!userId.HasValue)
        {
            context.Fail();
            return;
        }

        try
        {
            // Only query database when status verification is required
            var user = await _cachingService.GetUserByIdAsync(userId.Value, _serviceScopeFactory);
            if (user == null)
            {
                context.Fail();
                return;
            }

            // Handle status requirements
            if (requirement.UserStatus.Value != user.Status)
            {
                if (user.Status == UserStatus.Suspended && user.SuspendedUntil.HasValue && user.SuspendedUntil.Value <= DateTime.UtcNow)
                {
                    // Auto-unsuspend user
                    await _userService.UnsuspendUserAsync(userId.Value);
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
