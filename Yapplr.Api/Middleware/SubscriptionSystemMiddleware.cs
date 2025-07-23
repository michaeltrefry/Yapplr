using Yapplr.Api.Services;

namespace Yapplr.Api.Middleware;

/// <summary>
/// Middleware to check if subscription system is enabled and block access to subscription endpoints when disabled
/// </summary>
public class SubscriptionSystemMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SubscriptionSystemMiddleware> _logger;

    public SubscriptionSystemMiddleware(RequestDelegate next, ILogger<SubscriptionSystemMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISystemConfigurationService configService)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        
        // Check if this is a subscription-related endpoint
        if (IsSubscriptionEndpoint(path))
        {
            // Check if subscription system is enabled
            var isEnabled = await configService.IsSubscriptionSystemEnabledAsync();
            
            if (!isEnabled)
            {
                _logger.LogWarning("Subscription system is disabled. Blocking access to: {Path}", path);
                
                context.Response.StatusCode = 404; // Return 404 to make endpoints appear non-existent
                await context.Response.WriteAsync("Not Found");
                return;
            }
        }

        await _next(context);
    }

    private static bool IsSubscriptionEndpoint(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        // List of subscription-related endpoints to block when system is disabled
        var subscriptionPaths = new[]
        {
            "/api/subscriptions",
            "/api/admin/subscriptions" // Admin subscription management should still be accessible
        };

        // Check if path starts with any subscription path (but exclude admin paths)
        return subscriptionPaths.Any(subPath => 
            path.StartsWith(subPath) && !path.StartsWith("/api/admin/subscriptions"));
    }
}
