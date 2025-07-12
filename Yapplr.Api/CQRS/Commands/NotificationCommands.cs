namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to send a push notification
/// </summary>
public record SendNotificationCommand : BaseCommand
{
    public required int TargetUserId { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public Dictionary<string, string>? Data { get; init; }
    public string NotificationType { get; init; } = "generic";
    public int Priority { get; init; } = 5; // 1 = highest, 10 = lowest
}

/// <summary>
/// Command to send a message notification
/// </summary>
public record SendMessageNotificationCommand : BaseCommand
{
    public required int TargetUserId { get; init; }
    public required string SenderUsername { get; init; }
    public required string MessageContent { get; init; }
    public required int ConversationId { get; init; }
}

/// <summary>
/// Command to send a post interaction notification (like, comment, etc.)
/// </summary>
public record SendPostInteractionNotificationCommand : BaseCommand
{
    public required int TargetUserId { get; init; }
    public required int PostId { get; init; }
    public required string InteractionType { get; init; } // "like", "comment", "share"
    public required string ActorUsername { get; init; }
    public string? CommentText { get; init; }
}

/// <summary>
/// Command to send a follow notification
/// </summary>
public record SendFollowNotificationCommand : BaseCommand
{
    public required int TargetUserId { get; init; }
    public required string FollowerUsername { get; init; }
}

/// <summary>
/// Command to send a content moderation notification
/// </summary>
public record SendContentModerationNotificationCommand : BaseCommand
{
    public required int TargetUserId { get; init; }
    public required string ContentType { get; init; } // "post", "comment"
    public required int ContentId { get; init; }
    public required string Action { get; init; } // "hidden", "flagged", "approved"
    public string? Reason { get; init; }
    public bool AllowAppeal { get; init; } = true;
}

/// <summary>
/// Command to send a system announcement notification
/// </summary>
public record SendSystemAnnouncementCommand : BaseCommand
{
    public required string Title { get; init; }
    public required string Message { get; init; }
    public List<int>? TargetUserIds { get; init; } // null = all users
    public string AnnouncementType { get; init; } = "general";
}
