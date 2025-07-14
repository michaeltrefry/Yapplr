using System.Security.Claims;
using Yapplr.Api.Common;

namespace Yapplr.Api.Middleware;

public class UserActivityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICachingService _cachingService;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public UserActivityMiddleware(RequestDelegate next, ICachingService cachingService, IServiceScopeFactory serviceScopeFactory)
    {
        _next = next;
        _cachingService = cachingService;
        _serviceScopeFactory = serviceScopeFactory;
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
                        var user = await _cachingService.GetUserByIdAsync(userId, _serviceScopeFactory);
                        if (user != null && user.LastSeenAt < DateTime.UtcNow.AddSeconds(-5))
                        {
                            user.LastSeenAt = DateTime.UtcNow;
                            await _cachingService.SaveUserAsync(user, _serviceScopeFactory);
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
