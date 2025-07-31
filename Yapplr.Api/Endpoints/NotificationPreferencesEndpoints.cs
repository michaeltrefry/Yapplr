using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yapplr.Api.DTOs;
using Yapplr.Api.Extensions;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Notifications;

namespace Yapplr.Api.Endpoints;

public static class NotificationPreferencesEndpoints
{
    public static void MapNotificationPreferencesEndpoints(this IEndpointRouteBuilder app)
    {
        var preferences = app.MapGroup("/api/notification-preferences")
            .WithTags("Notification Preferences")
            .RequireAuthorization();

        // Get user's notification preferences
        preferences.MapGet("/", GetNotificationPreferences)
            .WithName("GetNotificationPreferences")
            .WithSummary("Get current user's notification preferences")
            .Produces<NotificationPreferences>(200)
            .Produces(401);

        // Update user's notification preferences
        preferences.MapPut("/", UpdateNotificationPreferences)
            .WithName("UpdateNotificationPreferences")
            .WithSummary("Update current user's notification preferences")
            .Produces<NotificationPreferences>(200)
            .Produces(400)
            .Produces(401);

        // Get delivery status for user's notifications
        preferences.MapGet("/delivery-status", GetDeliveryStatus)
            .WithName("GetDeliveryStatus")
            .WithSummary("Get delivery status for user's recent notifications")
            .Produces<List<NotificationDeliveryConfirmation>>(200)
            .Produces(401);

        // Get notification history
        preferences.MapGet("/history", GetNotificationHistory)
            .WithName("GetNotificationHistory")
            .WithSummary("Get user's notification history")
            .Produces<List<NotificationHistory>>(200)
            .Produces(401);

        // Get undelivered notifications
        preferences.MapGet("/undelivered", GetUndeliveredNotifications)
            .WithName("GetUndeliveredNotifications")
            .WithSummary("Get user's undelivered notifications")
            .Produces<List<NotificationHistory>>(200)
            .Produces(401);

        // Replay missed notifications
        preferences.MapPost("/replay", ReplayMissedNotifications)
            .WithName("ReplayMissedNotifications")
            .WithSummary("Replay missed notifications for the current user")
            .Produces(200)
            .Produces(401);

        // Confirm notification delivery (for client-side confirmation)
        preferences.MapPost("/confirm-delivery/{notificationId}", ConfirmDelivery)
            .WithName("ConfirmNotificationDelivery")
            .WithSummary("Confirm that a notification was delivered")
            .Produces(200)
            .Produces(404)
            .Produces(401);

        // Confirm notification read (for read receipts)
        preferences.MapPost("/confirm-read/{notificationId}", ConfirmRead)
            .WithName("ConfirmNotificationRead")
            .WithSummary("Confirm that a notification was read")
            .Produces(200)
            .Produces(404)
            .Produces(401);

        // Get delivery statistics
        preferences.MapGet("/stats", GetDeliveryStats)
            .WithName("GetDeliveryStats")
            .WithSummary("Get delivery statistics for the current user")
            .Produces<Dictionary<string, object>>(200)
            .Produces(401);
    }

    private static async Task<IResult> GetNotificationPreferences(
        ClaimsPrincipal user,
        INotificationPreferencesService preferencesService)
    {
        try
        {
            var userId = user.GetUserId(true);
            var preferences = await preferencesService.GetUserPreferencesAsync(userId);
            return Results.Ok(preferences);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get notification preferences: {ex.Message}");
        }
    }

    private static async Task<IResult> UpdateNotificationPreferences(
        ClaimsPrincipal user,
        UpdateNotificationPreferencesDto updateDto,
        INotificationPreferencesService preferencesService)
    {
        try
        {
            var userId = user.GetUserId(true);
            var preferences = await preferencesService.UpdateUserPreferencesAsync(userId, updateDto);
            return Results.Ok(preferences);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to update notification preferences: {ex.Message}");
        }
    }

    private static async Task<IResult> GetDeliveryStatus(
        ClaimsPrincipal user,
        INotificationService notificationService,
        [FromQuery] int count = 50)
    {
        try
        {
            var userId = user.GetUserId(true);
            var status = await notificationService.GetDeliveryStatusAsync(userId, count);
            return Results.Ok(status);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get delivery status: {ex.Message}");
        }
    }

    private static async Task<IResult> GetNotificationHistory(
        ClaimsPrincipal user,
        INotificationService notificationService,
        [FromQuery] int count = 100)
    {
        try
        {
            var userId = user.GetUserId(true);
            var history = await notificationService.GetNotificationHistoryAsync(userId, count);
            return Results.Ok(history);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get notification history: {ex.Message}");
        }
    }

    private static async Task<IResult> GetUndeliveredNotifications(
        ClaimsPrincipal user,
        INotificationService notificationService)
    {
        try
        {
            var userId = user.GetUserId(true);
            var undelivered = await notificationService.GetUndeliveredNotificationsAsync(userId);
            return Results.Ok(undelivered);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get undelivered notifications: {ex.Message}");
        }
    }

    private static async Task<IResult> ReplayMissedNotifications(
        ClaimsPrincipal user,
        INotificationService notificationService)
    {
        try
        {
            var userId = user.GetUserId(true);
            await notificationService.ReplayMissedNotificationsAsync(userId);
            return Results.Ok(new { message = "Missed notifications replayed successfully" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to replay missed notifications: {ex.Message}");
        }
    }

    private static async Task<IResult> ConfirmDelivery(
        string notificationId,
        INotificationService notificationService)
    {
        try
        {
            await notificationService.ConfirmDeliveryAsync(notificationId);
            return Results.Ok(new { message = "Delivery confirmed" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to confirm delivery: {ex.Message}");
        }
    }

    private static async Task<IResult> ConfirmRead(
        string notificationId,
        INotificationService notificationService)
    {
        try
        {
            await notificationService.ConfirmReadAsync(notificationId);
            return Results.Ok(new { message = "Read confirmed" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to confirm read: {ex.Message}");
        }
    }

    private static async Task<IResult> GetDeliveryStats(
        ClaimsPrincipal user,
        INotificationService notificationService,
        [FromQuery] int? timeWindowHours = null)
    {
        try
        {
            var userId = user.GetUserId(true);
            var timeWindow = timeWindowHours.HasValue ? TimeSpan.FromHours(timeWindowHours.Value) : (TimeSpan?)null;
            var stats = await notificationService.GetDeliveryStatsAsync(userId, timeWindow);
            return Results.Ok(stats);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get delivery stats: {ex.Message}");
        }
    }
}
