using Microsoft.AspNetCore.SignalR;

namespace Yapplr.Api.Hubs;

/// <summary>
/// Extension methods for sending notifications through SignalR
/// </summary>
public static class NotificationHubExtensions
{
    /// <summary>
    /// Send a notification to a specific user
    /// </summary>
    public static async Task SendNotificationToUserAsync(
        this IHubContext<NotificationHub> hubContext,
        int userId,
        string type,
        string title,
        string body,
        object? data = null)
    {
        var userGroup = $"user_{userId}";
        await hubContext.Clients.Group(userGroup).SendAsync("Notification", new
        {
            type,
            title,
            body,
            data,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Send a message notification to conversation participants
    /// </summary>
    public static async Task SendMessageToConversationAsync(
        this IHubContext<NotificationHub> hubContext,
        int conversationId,
        object messageData)
    {
        var conversationGroup = $"conversation_{conversationId}";
        await hubContext.Clients.Group(conversationGroup).SendAsync("NewMessage", messageData);
    }

    /// <summary>
    /// Send notifications to multiple users
    /// </summary>
    public static async Task SendNotificationToUsersAsync(
        this IHubContext<NotificationHub> hubContext,
        List<int> userIds,
        string type,
        string title,
        string body,
        object? data = null)
    {
        var userGroups = userIds.Select(id => $"user_{id}").ToList();
        await hubContext.Clients.Groups(userGroups).SendAsync("Notification", new
        {
            type,
            title,
            body,
            data,
            timestamp = DateTime.UtcNow
        });
    }
}