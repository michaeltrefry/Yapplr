using System.Security.Claims;
using Yapplr.Api.Services;

namespace Yapplr.Api.Middleware;

public class UserActivityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IUserCacheService _userCacheService;

    public UserActivityMiddleware(RequestDelegate next, IUserCacheService userCacheService)
    {
        _next = next;
        _userCacheService = userCacheService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Update user activity if user is authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                // Use a background task to avoid blocking the request
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var user = await _userCacheService.GetUserByIdAsync(userId);
                        if (user != null && user.LastSeenAt < DateTime.UtcNow.AddSeconds(-5))
                        {
                            user.LastSeenAt = DateTime.UtcNow;
                            await _userCacheService.SaveUserAsync(user);
                        }
                    }
                    catch
                    {
                        // Silently ignore errors to avoid affecting the main request
                    }
                });
            }
        }

        await _next(context);
    }
}
