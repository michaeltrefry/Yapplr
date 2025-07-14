using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yapplr.Api.Extensions;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Unified;
using Yapplr.Api.DTOs;

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
            .Produces<NotificationStats>(200)
            .Produces(401);

        // Performance insights endpoint removed - functionality integrated into main metrics

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
            .Produces<QueueStats>(200)
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
        IUnifiedNotificationService notificationService,
        [FromQuery] int? timeWindowMinutes = null)
    {
        try
        {
            var stats = await notificationService.GetStatsAsync();
            return Results.Ok(stats);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get notification metrics: {ex.Message}");
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
        INotificationQueue queueService)
    {
        try
        {
            var stats = await queueService.GetQueueStatsAsync();
            return Results.Ok(stats);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get queue stats: {ex.Message}");
        }
    }

    private static async Task<IResult> GetHealthCheck(
        IUnifiedNotificationService notificationService,
        ISignalRConnectionPool connectionPool,
        INotificationQueue queueService)
    {
        try
        {
            var healthReport = await notificationService.GetHealthReportAsync();

            // Add connection pool health
            var connectionStats = await connectionPool.GetStatsAsync();
            var connectionHealth = new
            {
                active_users = connectionStats.ActiveUsers,
                total_connections = connectionStats.TotalConnections,
                status = connectionStats.TotalConnections > 0 ? "healthy" : "no_connections"
            };

            // Add queue health
            var queueStats = await queueService.GetQueueStatsAsync();
            var queueHealth = new
            {
                currently_queued = queueStats.CurrentlyQueued,
                total_delivered = queueStats.TotalDelivered,
                delivery_success_rate = queueStats.DeliverySuccessRate,
                status = queueStats.DeliverySuccessRate >= 95 ? "healthy" :
                        queueStats.DeliverySuccessRate >= 80 ? "degraded" : "unhealthy"
            };

            var healthData = new Dictionary<string, object>
            {
                ["overall_health"] = healthReport.IsHealthy,
                ["notification_system"] = healthReport.ComponentHealth,
                ["connection_pool"] = connectionHealth,
                ["queue"] = queueHealth,
                ["last_checked"] = healthReport.LastChecked,
                ["issues"] = healthReport.Issues
            };

            return Results.Ok(healthData);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get health check data: {ex.Message}");
        }
    }

    private static async Task<IResult> ResetMetrics(
        IUnifiedNotificationService notificationService,
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
            await notificationService.RefreshSystemAsync();
            return Results.Ok(new { message = "Notification system refreshed successfully" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to refresh notification system: {ex.Message}");
        }
    }

    private static async Task<IResult> ProcessQueue(
        INotificationQueue queueService,
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
