using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;

namespace Yapplr.Api.Middleware;

public class UserActivityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public UserActivityMiddleware(RequestDelegate next, IServiceScopeFactory serviceScopeFactory)
    {
        _next = next;
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
                        using var scope = _serviceScopeFactory.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<YapplrDbContext>();
                        
                        var user = await dbContext.Users.FindAsync(userId);
                        if (user != null)
                        {
                            user.LastSeenAt = DateTime.UtcNow;
                            await dbContext.SaveChangesAsync();
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
