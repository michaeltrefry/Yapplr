using System.Security.Claims;
using Yapplr.Api.DTOs;
using Yapplr.Api.Extensions;
using Yapplr.Api.Services;

namespace Yapplr.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        var notifications = app.MapGroup("/api/notifications").WithTags("Notifications");

        // Get user notifications with pagination
        notifications.MapGet("/", async (ClaimsPrincipal user, INotificationService notificationService, int page = 1, int pageSize = 25) => 
            {
                var userId = user.GetUserId(true);
                var result = await notificationService.GetUserNotificationsAsync(userId, page, pageSize);
                
                return Results.Ok(result);
            })
        .WithName("GetNotifications")
        .WithSummary("Get user notifications with pagination")
        .RequireAuthorization("ActiveUser")
        .Produces<NotificationListDto>(200)
        .Produces(401);

        // Get unread notification count
        notifications.MapGet("/unread-count", async (ClaimsPrincipal user, INotificationService notificationService) =>
        {
            var userId = user.GetUserId(true);
            var unreadCount = await notificationService.GetUnreadNotificationCountAsync(userId);

            return Results.Ok(new { unreadCount });
        })
        .WithName("GetUnreadNotificationCount")
        .WithSummary("Get count of unread notifications for user")
        .Produces(200)
        .Produces(401);

        // Get combined unread counts for notifications and messages
        notifications.MapGet("/combined-unread-count", async (ClaimsPrincipal user, INotificationService notificationService, IMessageService messageService) =>
        {
            var userId = user.GetUserId(true);

            // Execute queries sequentially to avoid DbContext concurrency issues
            var notificationCount = await notificationService.GetUnreadNotificationCountAsync(userId);
            var messageCount = await messageService.GetTotalUnreadMessageCountAsync(userId);

            return Results.Ok(new CombinedUnreadCountDto
            {
                UnreadNotificationCount = notificationCount,
                UnreadMessageCount = messageCount
            });
        })
        .WithName("GetCombinedUnreadCount")
        .WithSummary("Get combined count of unread notifications and messages for user")
        .RequireAuthorization("ActiveUser")
        .Produces<CombinedUnreadCountDto>(200)
        .Produces(401);

        // Mark notification as read
        notifications.MapPut("/{notificationId:int}/read", async (int notificationId, ClaimsPrincipal user, INotificationService notificationService) =>
        {
            var userId = user.GetUserId(true);
            var success = await notificationService.MarkNotificationAsReadAsync(notificationId, userId);
            
            return success ? Results.Ok(new { message = "Notification marked as read" }) : Results.NotFound(new { message = "Notification not found" });
        })
        .WithName("MarkNotificationAsRead")
        .WithSummary("Mark a specific notification as read")
        .RequireAuthorization("ActiveUser")
        .Produces(200)
        .Produces(404)
        .Produces(401);

        // Mark all notifications as read
        notifications.MapPut("/read-all", async (ClaimsPrincipal user, INotificationService notificationService) =>
        {
            var userId = user.GetUserId(true);
            var success = await notificationService.MarkAllNotificationsAsReadAsync(userId);
            
            return Results.Ok(new { message = success ? "All notifications marked as read" : "No unread notifications found" });
        })
        .WithName("MarkAllNotificationsAsRead")
        .WithSummary("Mark all notifications as read for the user")
        .RequireAuthorization("ActiveUser")
        .Produces(200)
        .Produces(401);

        // Test notification endpoints - only available in development
        if (app.Environment.IsDevelopment())
        {
            // Test notification endpoint (Firebase + SignalR fallback)
            notifications.MapPost("/test", async (ClaimsPrincipal user, ICompositeNotificationService notificationService) =>
            {
                var userId = user.GetUserId(true);

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
            .RequireAuthorization("ActiveUser")
            .Produces(200)
            .Produces(400)
            .Produces(401);

            // Test Firebase-specific notification endpoint
            notifications.MapPost("/test-firebase", async (ClaimsPrincipal user, IFirebaseService firebaseService, IUserService userService) =>
            {
                var userId = user.GetUserId(true);
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
            .RequireAuthorization("ActiveUser")
            .Produces(200)
            .Produces(400)
            .Produces(401);

            // Test Firebase service with mock token (for simulator testing)
            notifications.MapPost("/test-firebase-mock", async (IFirebaseService firebaseService) =>
            {
                // Use a mock Expo push token format for testing
                var mockToken = "ExponentPushToken[xxxxxxxxxxxxxxxxxxxxxx]";

                var success = await firebaseService.SendTestNotificationAsync(mockToken);

                return Results.Ok(new {
                    success = success,
                    message = success ? "Firebase test with mock token sent successfully" : "Firebase test with mock token failed",
                    mockToken = mockToken,
                    note = "This tests Firebase service functionality with a mock token (will fail to deliver but tests the service)"
                });
            })
            .WithName("TestFirebaseMock")
            .WithSummary("Test Firebase service with mock token (for simulator testing)")
            .RequireAuthorization("ActiveUser")
            .Produces(200)
            .Produces(401);

            // Test SignalR-specific notification endpoint
            notifications.MapPost("/test-signalr", async (ClaimsPrincipal user, SignalRNotificationService signalRService) =>
            {
                var userId = user.GetUserId(true);

                var success = await signalRService.SendTestNotificationAsync(userId);

                return Results.Ok(new {
                    success = success,
                    message = success ? "SignalR test notification sent successfully" : "Failed to send SignalR test notification"
                });
            })
            .WithName("TestSignalRNotification")
            .WithSummary("Send a test SignalR notification directly (for debugging)")
            .RequireAuthorization("ActiveUser")
            .Produces(200)
            .Produces(400)
            .Produces(401);

            // Test Expo-specific notification endpoint
            notifications.MapPost("/test-expo", async (ClaimsPrincipal user, ExpoNotificationService expoService, IUserService userService) =>
            {
                var userId = user.GetUserId(true);
                var currentUser = await userService.GetUserByIdAsync(userId);

                if (currentUser?.ExpoPushToken == null || string.IsNullOrEmpty(currentUser.ExpoPushToken))
                {
                    return Results.BadRequest(new { error = "No Expo push token found for user" });
                }

                var success = await expoService.SendTestNotificationAsync(userId);

                return Results.Ok(new {
                    success = success,
                    message = success ? "Expo test notification sent successfully" : "Failed to send Expo test notification",
                    expoPushTokenLength = currentUser.ExpoPushToken.Length,
                    expoPushTokenStart = currentUser.ExpoPushToken.Substring(0, Math.Min(30, currentUser.ExpoPushToken.Length)) + "..."
                });
            })
            .WithName("TestExpoNotification")
            .WithSummary("Send a test Expo notification directly (for debugging)")
            .RequireAuthorization("ActiveUser")
            .Produces(200)
            .Produces(400)
            .Produces(401);
        }
    }
}
