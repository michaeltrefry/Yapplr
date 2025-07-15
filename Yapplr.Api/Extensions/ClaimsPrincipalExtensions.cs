using System.Security.Claims;
using Yapplr.Api.Data;
using Yapplr.Api.Models;

namespace Yapplr.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user, bool throwException = false)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            return userId;
        if (throwException)
        {
            throw new BadHttpRequestException("Missing user id claim.");
        }
        return -1;
    }

    public static int? GetUserIdOrNull(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            return userId;
        return null;
    }

    public static bool IsAuthenticated(this ClaimsPrincipal user)
    {
        return user?.Identity?.IsAuthenticated == true;
    }

    public static bool HasRole(this ClaimsPrincipal user, string role)
    {
        return user.IsInRole(role);
    }

    /// <summary>
    /// Get the user's role from JWT claims
    /// </summary>
    public static UserRole? GetUserRole(this ClaimsPrincipal user)
    {
        var roleClaim = user.FindFirst(ClaimTypes.Role);
        if (roleClaim != null && Enum.TryParse<UserRole>(roleClaim.Value, out var role))
            return role;
        return null;
    }

    /// <summary>
    /// Check if user has the specified role or higher
    /// </summary>
    public static bool HasRoleOrHigher(this ClaimsPrincipal user, UserRole minimumRole)
    {
        var userRole = user.GetUserRole();
        return userRole.HasValue && userRole.Value >= minimumRole;
    }

    /// <summary>
    /// Check if user is admin or moderator using JWT claims
    /// </summary>
    public static bool IsAdminOrModerator(this ClaimsPrincipal user)
    {
        return user.HasRoleOrHigher(UserRole.Moderator);
    }

    /// <summary>
    /// Check if user is admin using JWT claims
    /// </summary>
    public static bool IsAdmin(this ClaimsPrincipal user)
    {
        return user.HasRoleOrHigher(UserRole.Admin);
    }

    /// <summary>
    /// Check if user is system using JWT claims
    /// </summary>
    public static bool IsSystem(this ClaimsPrincipal user)
    {
        return user.HasRoleOrHigher(UserRole.System);
    }

    /// <summary>
    /// Validate that JWT role claims match current database role (for critical operations)
    /// Returns true if claims are valid, false if token refresh is needed
    /// </summary>
    public static async Task<bool> ValidateRoleClaimsAsync(this ClaimsPrincipal user, YapplrDbContext context)
    {
        var userId = user.GetUserIdOrNull();
        if (!userId.HasValue) return false;

        var jwtRole = user.GetUserRole();
        if (!jwtRole.HasValue) return false;

        var dbUser = await context.Users.FindAsync(userId.Value);
        if (dbUser == null) return false;

        return dbUser.Role == jwtRole.Value;
    }
}