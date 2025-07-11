using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yapplr.Api.Extensions;
using Yapplr.Api.Services;

namespace Yapplr.Api.Endpoints;

public static class MetricsEndpoints
{
    public static void MapMetricsEndpoints(this IEndpointRouteBuilder app)
    {
        var metrics = app.MapGroup("/api/metrics")
            .WithTags("Metrics")
            .RequireAuthorization(); // Require authentication for all metrics endpoints

        // Notification metrics
        metrics.MapGet("/notifications", GetNotificationMetrics)
            .WithName("GetNotificationMetrics")
            .WithSummary("Get notification delivery metrics")
            .Produces<NotificationMetrics>(200)
            .Produces(401);

        metrics.MapGet("/notifications/recent", GetRecentDeliveries)
            .WithName("GetRecentDeliveries")
            .WithSummary("Get recent notification deliveries")
            .Produces<List<DeliveryMetric>>(200)
            .Produces(401);

        metrics.MapGet("/notifications/insights", GetPerformanceInsights)
            .WithName("GetPerformanceInsights")
            .WithSummary("Get performance insights and recommendations")
            .Produces<Dictionary<string, object>>(200)
            .Produces(401);

        // Connection pool metrics
        metrics.MapGet("/connections", GetConnectionPoolStats)
            .WithName("GetConnectionPoolStats")
            .WithSummary("Get SignalR connection pool statistics")
            .Produces<ConnectionPoolStats>(200)
            .Produces(401);

        metrics.MapGet("/connections/users", GetUserConnectionStats)
            .WithName("GetUserConnectionStats")
            .WithSummary("Get per-user connection statistics")
            .Produces<Dictionary<int, int>>(200)
            .Produces(401);

        // Queue metrics
        metrics.MapGet("/queue", GetQueueStats)
            .WithName("GetQueueStats")
            .WithSummary("Get notification queue statistics")
            .Produces<NotificationQueueStats>(200)
            .Produces(401);

        metrics.MapGet("/queue/pending", GetPendingNotifications)
            .WithName("GetPendingNotifications")
            .WithSummary("Get pending notifications in queue")
            .Produces<List<QueuedNotification>>(200)
            .Produces(401);

        // Health check endpoint
        metrics.MapGet("/health", GetHealthCheck)
            .WithName("GetNotificationHealthCheck")
            .WithSummary("Get notification system health status")
            .Produces<Dictionary<string, object>>(200)
            .Produces(401);

        // Admin endpoints (require additional authorization)
        metrics.MapPost("/reset", ResetMetrics)
            .WithName("ResetMetrics")
            .WithSummary("Reset all metrics (admin only)")
            .Produces(200)
            .Produces(401)
            .Produces(403);

        metrics.MapPost("/queue/process", ProcessQueue)
            .WithName("ProcessQueue")
            .WithSummary("Manually trigger queue processing (admin only)")
            .Produces(200)
            .Produces(401)
            .Produces(403);
    }

    private static async Task<IResult> GetNotificationMetrics(
        INotificationMetricsService metricsService,
        [FromQuery] int? timeWindowMinutes = null)
    {
        try
        {
            var timeWindow = timeWindowMinutes.HasValue 
                ? TimeSpan.FromMinutes(timeWindowMinutes.Value) 
                : (TimeSpan?)null;

            var metrics = await metricsService.GetMetricsAsync(timeWindow);
            return Results.Ok(metrics);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get notification metrics: {ex.Message}");
        }
    }

    private static async Task<IResult> GetRecentDeliveries(
        INotificationMetricsService metricsService,
        [FromQuery] int count = 100)
    {
        try
        {
            var deliveries = await metricsService.GetRecentDeliveriesAsync(count);
            return Results.Ok(deliveries);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get recent deliveries: {ex.Message}");
        }
    }

    private static async Task<IResult> GetPerformanceInsights(
        INotificationMetricsService metricsService)
    {
        try
        {
            var insights = await metricsService.GetPerformanceInsightsAsync();
            return Results.Ok(insights);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get performance insights: {ex.Message}");
        }
    }

    private static async Task<IResult> GetConnectionPoolStats(
        ISignalRConnectionPool connectionPool)
    {
        try
        {
            var stats = await connectionPool.GetStatsAsync();
            return Results.Ok(stats);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get connection pool stats: {ex.Message}");
        }
    }

    private static async Task<IResult> GetUserConnectionStats(
        ISignalRConnectionPool connectionPool)
    {
        try
        {
            var stats = await connectionPool.GetUserConnectionStatsAsync();
            return Results.Ok(stats);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get user connection stats: {ex.Message}");
        }
    }

    private static async Task<IResult> GetQueueStats(
        INotificationQueueService queueService)
    {
        try
        {
            var stats = await queueService.GetStatsAsync();
            return Results.Ok(stats);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get queue stats: {ex.Message}");
        }
    }

    private static async Task<IResult> GetPendingNotifications(
        INotificationQueueService queueService,
        ClaimsPrincipal user)
    {
        try
        {
            var userId = user.GetUserId(true);
            var notifications = await queueService.GetPendingNotificationsAsync(userId);
            return Results.Ok(notifications);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get pending notifications: {ex.Message}");
        }
    }

    private static async Task<IResult> GetHealthCheck(
        INotificationMetricsService metricsService,
        ISignalRConnectionPool connectionPool,
        INotificationQueueService queueService)
    {
        try
        {
            var healthData = await metricsService.GetHealthCheckDataAsync();
            
            // Add connection pool health
            var connectionStats = await connectionPool.GetStatsAsync();
            healthData["connection_pool"] = new
            {
                active_users = connectionStats.ActiveUsers,
                total_connections = connectionStats.TotalConnections,
                status = connectionStats.TotalConnections > 0 ? "healthy" : "no_connections"
            };

            // Add queue health
            var queueStats = await queueService.GetStatsAsync();
            healthData["queue"] = new
            {
                pending_count = queueStats.PendingInMemory,
                queue_size = queueStats.QueueSize,
                delivery_rate = queueStats.DeliveryRate,
                status = queueStats.DeliveryRate >= 95 ? "healthy" : 
                        queueStats.DeliveryRate >= 80 ? "degraded" : "unhealthy"
            };

            return Results.Ok(healthData);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get health check data: {ex.Message}");
        }
    }

    private static async Task<IResult> ResetMetrics(
        INotificationMetricsService metricsService,
        ClaimsPrincipal user)
    {
        // Simple admin check - in production, you'd want proper role-based authorization
        var username = user.FindFirst(ClaimTypes.Name)?.Value;
        if (username != "admin") // Replace with proper admin check
        {
            return Results.Forbid();
        }

        try
        {
            await metricsService.ResetMetricsAsync();
            return Results.Ok(new { message = "Metrics reset successfully" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to reset metrics: {ex.Message}");
        }
    }

    private static async Task<IResult> ProcessQueue(
        INotificationQueueService queueService,
        ClaimsPrincipal user)
    {
        // Simple admin check - in production, you'd want proper role-based authorization
        var username = user.FindFirst(ClaimTypes.Name)?.Value;
        if (username != "admin") // Replace with proper admin check
        {
            return Results.Forbid();
        }

        try
        {
            await queueService.ProcessPendingNotificationsAsync();
            return Results.Ok(new { message = "Queue processing triggered successfully" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to process queue: {ex.Message}");
        }
    }
}
