using Yapplr.Api.Models;

namespace Yapplr.Api.Services.Notifications;

/// <summary>
/// Centralized service for building consistent notification content across all delivery channels.
/// Eliminates duplicate content logic in Firebase, SignalR, and Expo services.
/// </summary>
public class NotificationContentBuilder : INotificationContentBuilder
{
    /// <summary>
    /// Builds content for a like notification
    /// Using "New React" as the consistent title across all services
    /// </summary>
    public NotificationContent BuildLikeNotification(string likerUsername, int postId)
    {
        return new NotificationContent(
            title: "New React",
            body: $"@{likerUsername} liked your post",
            notificationType: "like"
        ).WithData(new Dictionary<string, string>
        {
            ["postId"] = postId.ToString(),
            ["likerUsername"] = likerUsername
        });
    }

    /// <summary>
    /// Builds content for a comment like notification
    /// Using "New React" as the consistent title across all services
    /// </summary>
    public NotificationContent BuildCommentLikeNotification(string likerUsername, int postId, int commentId)
    {
        return new NotificationContent(
            title: "New React",
            body: $"@{likerUsername} liked your comment",
            notificationType: "comment_like"
        ).WithData(new Dictionary<string, string>
        {
            ["postId"] = postId.ToString(),
            ["commentId"] = commentId.ToString(),
            ["likerUsername"] = likerUsername
        });
    }

    /// <summary>
    /// Builds content for a comment notification
    /// Using "New Comment" as the consistent title across all services
    /// </summary>
    public NotificationContent BuildCommentNotification(string commenterUsername, int postId, int commentId)
    {
        return new NotificationContent(
            title: "New Comment",
            body: $"@{commenterUsername} commented on your post",
            notificationType: "comment"
        ).WithData(new Dictionary<string, string>
        {
            ["postId"] = postId.ToString(),
            ["commentId"] = commentId.ToString(),
            ["commenterUsername"] = commenterUsername
        });
    }

    /// <summary>
    /// Builds content for a follow notification
    /// Using "New Follower" as the consistent title across all services
    /// </summary>
    public NotificationContent BuildFollowNotification(string followerUsername)
    {
        return new NotificationContent(
            title: "New Follower",
            body: $"@{followerUsername} started following you",
            notificationType: "follow"
        ).WithData(new Dictionary<string, string>
        {
            ["followerUsername"] = followerUsername
        });
    }

    /// <summary>
    /// Builds content for a follow request notification
    /// Using "Follow Request" as the consistent title across all services
    /// </summary>
    public NotificationContent BuildFollowRequestNotification(string requesterUsername)
    {
        return new NotificationContent(
            title: "Follow Request",
            body: $"@{requesterUsername} wants to follow you",
            notificationType: "follow_request"
        ).WithData(new Dictionary<string, string>
        {
            ["requesterUsername"] = requesterUsername
        });
    }

    /// <summary>
    /// Builds content for a follow request approved notification
    /// </summary>
    public NotificationContent BuildFollowRequestApprovedNotification(string approverUsername)
    {
        return new NotificationContent(
            title: "Follow Request Approved",
            body: $"@{approverUsername} approved your follow request",
            notificationType: "follow_request_approved"
        ).WithData(new Dictionary<string, string>
        {
            ["approverUsername"] = approverUsername
        });
    }

    /// <summary>
    /// Builds content for a message notification
    /// Using "New Message" as the consistent title across all services
    /// </summary>
    public NotificationContent BuildMessageNotification(string senderUsername, string messageContent, int conversationId)
    {
        return new NotificationContent(
            title: "New Message",
            body: $"@{senderUsername}: {TruncateMessage(messageContent)}",
            notificationType: "message"
        ).WithData(new Dictionary<string, string>
        {
            ["conversationId"] = conversationId.ToString(),
            ["senderUsername"] = senderUsername
        });
    }

    /// <summary>
    /// Builds content for a mention notification
    /// Using SignalR's more descriptive approach as the standard
    /// </summary>
    public NotificationContent BuildMentionNotification(string mentionerUsername, int postId, int? commentId = null)
    {
        var title = "You were mentioned";
        var body = commentId.HasValue 
            ? $"@{mentionerUsername} mentioned you in a comment"
            : $"@{mentionerUsername} mentioned you in a post";

        var data = new Dictionary<string, string>
        {
            ["postId"] = postId.ToString(),
            ["mentionerUsername"] = mentionerUsername
        };

        if (commentId.HasValue)
        {
            data["commentId"] = commentId.Value.ToString();
        }

        return new NotificationContent(title, body, "mention").WithData(data);
    }

    /// <summary>
    /// Builds content for a reply notification
    /// </summary>
    public NotificationContent BuildReplyNotification(string replierUsername, int postId, int commentId)
    {
        return new NotificationContent(
            title: "New Reply",
            body: $"@{replierUsername} replied to your comment",
            notificationType: "reply"
        ).WithData(new Dictionary<string, string>
        {
            ["postId"] = postId.ToString(),
            ["commentId"] = commentId.ToString(),
            ["replierUsername"] = replierUsername
        });
    }

    /// <summary>
    /// Builds content for a repost notification
    /// </summary>
    public NotificationContent BuildRepostNotification(string reposterUsername, int postId)
    {
        return new NotificationContent(
            title: "New Repost",
            body: $"@{reposterUsername} reposted your post",
            notificationType: "repost"
        ).WithData(new Dictionary<string, string>
        {
            ["postId"] = postId.ToString(),
            ["reposterUsername"] = reposterUsername
        });
    }

    /// <summary>
    /// Builds content for a system message notification
    /// </summary>
    public NotificationContent BuildSystemMessageNotification(string title, string message, Dictionary<string, string>? additionalData = null)
    {
        var content = new NotificationContent(title, message, "systemMessage");
        
        if (additionalData != null)
        {
            content.WithData(additionalData);
        }

        return content;
    }

    /// <summary>
    /// Builds content for a video processing completed notification
    /// </summary>
    public NotificationContent BuildVideoProcessingCompletedNotification(int postId)
    {
        return new NotificationContent(
            title: "Video Ready",
            body: "Your video has been processed and is now available",
            notificationType: "VideoProcessingCompleted"
        ).WithData(new Dictionary<string, string>
        {
            ["postId"] = postId.ToString()
        });
    }

    /// <summary>
    /// Builds content for a test notification
    /// </summary>
    public NotificationContent BuildTestNotification(string? customMessage = null)
    {
        return new NotificationContent(
            title: "Test",
            body: customMessage ?? "Test notification",
            notificationType: "test"
        );
    }

    /// <summary>
    /// Truncates message content for display in notifications
    /// </summary>
    private static string TruncateMessage(string message, int maxLength = 50)
    {
        if (string.IsNullOrEmpty(message))
            return string.Empty;

        return message.Length <= maxLength ? message : message.Substring(0, maxLength) + "...";
    }
}
