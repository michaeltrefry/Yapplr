using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yapplr.Api.DTOs;
using Yapplr.Api.Services;

namespace Yapplr.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        var notifications = app.MapGroup("/api/notifications").WithTags("Notifications");

        // Get user notifications with pagination
        notifications.MapGet("/", [Authorize] async (ClaimsPrincipal user, INotificationService notificationService, int page = 1, int pageSize = 25) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await notificationService.GetUserNotificationsAsync(userId, page, pageSize);
            
            return Results.Ok(result);
        })
        .WithName("GetNotifications")
        .WithSummary("Get user notifications with pagination")
        .Produces<NotificationListDto>(200)
        .Produces(401);

        // Get unread notification count
        notifications.MapGet("/unread-count", [Authorize] async (ClaimsPrincipal user, INotificationService notificationService) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var unreadCount = await notificationService.GetUnreadNotificationCountAsync(userId);
            
            return Results.Ok(new { unreadCount });
        })
        .WithName("GetUnreadNotificationCount")
        .WithSummary("Get count of unread notifications for user")
        .Produces(200)
        .Produces(401);

        // Mark notification as read
        notifications.MapPut("/{notificationId:int}/read", [Authorize] async (int notificationId, ClaimsPrincipal user, INotificationService notificationService) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var success = await notificationService.MarkNotificationAsReadAsync(notificationId, userId);
            
            return success ? Results.Ok(new { message = "Notification marked as read" }) : Results.NotFound(new { message = "Notification not found" });
        })
        .WithName("MarkNotificationAsRead")
        .WithSummary("Mark a specific notification as read")
        .Produces(200)
        .Produces(404)
        .Produces(401);

        // Mark all notifications as read
        notifications.MapPut("/read-all", [Authorize] async (ClaimsPrincipal user, INotificationService notificationService) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var success = await notificationService.MarkAllNotificationsAsReadAsync(userId);
            
            return Results.Ok(new { message = success ? "All notifications marked as read" : "No unread notifications found" });
        })
        .WithName("MarkAllNotificationsAsRead")
        .WithSummary("Mark all notifications as read for the user")
        .Produces(200)
        .Produces(401);

        // Test notification endpoint (Firebase + SignalR fallback)
        notifications.MapPost("/test", [Authorize] async (ClaimsPrincipal user, ICompositeNotificationService notificationService) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var success = await notificationService.SendTestNotificationAsync(userId);

            return Results.Ok(new {
                success = success,
                message = success ? "Test notification sent successfully" : "Failed to send test notification",
                activeProvider = notificationService.ActiveProvider?.ProviderName ?? "None",
                availableProviders = (await notificationService.GetProviderStatusAsync()).Select(kvp => new { name = kvp.Key, available = kvp.Value })
            });
        })
        .WithName("TestNotification")
        .WithSummary("Send a test notification using the composite service (Firebase with SignalR fallback)")
        .Produces(200)
        .Produces(400)
        .Produces(401);

        // Test Firebase-specific notification endpoint
        notifications.MapPost("/test-firebase", [Authorize] async (ClaimsPrincipal user, IFirebaseService firebaseService, IUserService userService) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var currentUser = await userService.GetUserByIdAsync(userId);

            if (currentUser?.FcmToken == null || string.IsNullOrEmpty(currentUser.FcmToken))
            {
                return Results.BadRequest(new { error = "No FCM token found for user" });
            }

            var success = await firebaseService.SendTestNotificationAsync(currentUser.FcmToken);

            return Results.Ok(new {
                success = success,
                message = success ? "Firebase test notification sent successfully" : "Failed to send Firebase test notification",
                fcmTokenLength = currentUser.FcmToken.Length,
                fcmTokenStart = currentUser.FcmToken.Substring(0, Math.Min(30, currentUser.FcmToken.Length)) + "..."
            });
        })
        .WithName("TestFirebaseNotification")
        .WithSummary("Send a test Firebase notification directly (for debugging)")
        .Produces(200)
        .Produces(400)
        .Produces(401);

        // Test SignalR-specific notification endpoint
        notifications.MapPost("/test-signalr", [Authorize] async (ClaimsPrincipal user, SignalRNotificationService signalRService) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var success = await signalRService.SendTestNotificationAsync(userId);

            return Results.Ok(new {
                success = success,
                message = success ? "SignalR test notification sent successfully" : "Failed to send SignalR test notification"
            });
        })
        .WithName("TestSignalRNotification")
        .WithSummary("Send a test SignalR notification directly (for debugging)")
        .Produces(200)
        .Produces(400)
        .Produces(401);
    }
}
