using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yapplr.Api.Extensions;
using Yapplr.Api.Services;

namespace Yapplr.Api.Endpoints;

public static class UXEnhancementEndpoints
{
    public static void MapUXEnhancementEndpoints(this IEndpointRouteBuilder app)
    {
        var ux = app.MapGroup("/api/ux")
            .WithTags("UX Enhancements")
            .RequireAuthorization();

        // Offline notification endpoints
        ux.MapGet("/offline/notifications", GetOfflineNotifications)
            .WithName("GetOfflineNotifications")
            .WithSummary("Get offline notifications for the current user")
            .Produces<List<OfflineNotification>>(200)
            .Produces(401);

        ux.MapPost("/offline/process", ProcessOfflineNotifications)
            .WithName("ProcessOfflineNotifications")
            .WithSummary("Process offline notifications for the current user")
            .Produces(200)
            .Produces(401);

        ux.MapGet("/offline/status", GetConnectivityStatus)
            .WithName("GetConnectivityStatus")
            .WithSummary("Get connectivity status for the current user")
            .Produces<UserConnectivityStatus>(200)
            .Produces(401);

        ux.MapPost("/offline/mark-online", MarkUserOnline)
            .WithName("MarkUserOnline")
            .WithSummary("Mark the current user as online")
            .Produces(200)
            .Produces(401);

        ux.MapPost("/offline/mark-offline", MarkUserOffline)
            .WithName("MarkUserOffline")
            .WithSummary("Mark the current user as offline")
            .Produces(200)
            .Produces(401);

        // Compression stats
        ux.MapGet("/compression/stats", GetCompressionStats)
            .WithName("GetCompressionStats")
            .WithSummary("Get notification compression statistics")
            .Produces<Dictionary<string, object>>(200)
            .Produces(401);

        // Offline stats
        ux.MapGet("/offline/stats", GetOfflineStats)
            .WithName("GetOfflineStats")
            .WithSummary("Get offline notification statistics")
            .Produces<Dictionary<string, object>>(200)
            .Produces(401);

        // Admin endpoints
        ux.MapGet("/admin/connectivity", GetAllConnectivityStatus)
            .WithName("GetAllConnectivityStatus")
            .WithSummary("Get connectivity status for all users (admin only)")
            .Produces<List<UserConnectivityStatus>>(200)
            .Produces(401)
            .Produces(403);

        ux.MapPost("/admin/process-all-offline", ProcessAllOfflineNotifications)
            .WithName("ProcessAllOfflineNotifications")
            .WithSummary("Process offline notifications for all users (admin only)")
            .Produces(200)
            .Produces(401)
            .Produces(403);

        ux.MapPost("/admin/cleanup-expired", CleanupExpiredNotifications)
            .WithName("CleanupExpiredNotifications")
            .WithSummary("Cleanup expired offline notifications (admin only)")
            .Produces(200)
            .Produces(401)
            .Produces(403);
    }

    private static async Task<IResult> GetOfflineNotifications(
        ClaimsPrincipal user,
        IOfflineNotificationService offlineService)
    {
        try
        {
            var userId = user.GetUserId(true);
            var notifications = await offlineService.GetOfflineNotificationsAsync(userId);
            return Results.Ok(notifications);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get offline notifications: {ex.Message}");
        }
    }

    private static async Task<IResult> ProcessOfflineNotifications(
        ClaimsPrincipal user,
        IOfflineNotificationService offlineService)
    {
        try
        {
            var userId = user.GetUserId(true);
            await offlineService.ProcessOfflineNotificationsAsync(userId);
            return Results.Ok(new { message = "Offline notifications processed successfully" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to process offline notifications: {ex.Message}");
        }
    }

    private static async Task<IResult> GetConnectivityStatus(
        ClaimsPrincipal user,
        IOfflineNotificationService offlineService)
    {
        try
        {
            var userId = user.GetUserId(true);
            var status = await offlineService.GetUserConnectivityStatusAsync(userId);
            return Results.Ok(status);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get connectivity status: {ex.Message}");
        }
    }

    private static async Task<IResult> MarkUserOnline(
        ClaimsPrincipal user,
        IOfflineNotificationService offlineService,
        [FromQuery] string connectionType = "manual")
    {
        try
        {
            var userId = user.GetUserId(true);
            await offlineService.MarkUserOnlineAsync(userId, connectionType);
            return Results.Ok(new { message = "User marked as online" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to mark user as online: {ex.Message}");
        }
    }

    private static async Task<IResult> MarkUserOffline(
        ClaimsPrincipal user,
        IOfflineNotificationService offlineService)
    {
        try
        {
            var userId = user.GetUserId(true);
            await offlineService.MarkUserOfflineAsync(userId);
            return Results.Ok(new { message = "User marked as offline" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to mark user as offline: {ex.Message}");
        }
    }

    private static async Task<IResult> GetCompressionStats(
        INotificationCompressionService compressionService)
    {
        try
        {
            var stats = await compressionService.GetCompressionStatsAsync();
            return Results.Ok(stats);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get compression stats: {ex.Message}");
        }
    }

    private static async Task<IResult> GetOfflineStats(
        IOfflineNotificationService offlineService)
    {
        try
        {
            var stats = await offlineService.GetOfflineStatsAsync();
            return Results.Ok(stats);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get offline stats: {ex.Message}");
        }
    }

    private static async Task<IResult> GetAllConnectivityStatus(
        ClaimsPrincipal user,
        IOfflineNotificationService offlineService)
    {
        // Simple admin check - in production, you'd want proper role-based authorization
        var username = user.FindFirst(ClaimTypes.Name)?.Value;
        if (username != "admin") // Replace with proper admin check
        {
            return Results.Forbid();
        }

        try
        {
            var statuses = await offlineService.GetAllUserConnectivityStatusAsync();
            return Results.Ok(statuses);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get all connectivity status: {ex.Message}");
        }
    }

    private static async Task<IResult> ProcessAllOfflineNotifications(
        ClaimsPrincipal user,
        IOfflineNotificationService offlineService)
    {
        // Simple admin check - in production, you'd want proper role-based authorization
        var username = user.FindFirst(ClaimTypes.Name)?.Value;
        if (username != "admin") // Replace with proper admin check
        {
            return Results.Forbid();
        }

        try
        {
            await offlineService.ProcessAllOfflineNotificationsAsync();
            return Results.Ok(new { message = "All offline notifications processed successfully" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to process all offline notifications: {ex.Message}");
        }
    }

    private static async Task<IResult> CleanupExpiredNotifications(
        ClaimsPrincipal user,
        IOfflineNotificationService offlineService)
    {
        // Simple admin check - in production, you'd want proper role-based authorization
        var username = user.FindFirst(ClaimTypes.Name)?.Value;
        if (username != "admin") // Replace with proper admin check
        {
            return Results.Forbid();
        }

        try
        {
            await offlineService.CleanupExpiredNotificationsAsync();
            return Results.Ok(new { message = "Expired notifications cleaned up successfully" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to cleanup expired notifications: {ex.Message}");
        }
    }
}
