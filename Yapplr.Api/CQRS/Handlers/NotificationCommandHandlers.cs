using MassTransit;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Services;

namespace Yapplr.Api.CQRS.Handlers;

/// <summary>
/// Handler for generic notification sending commands
/// </summary>
public class SendNotificationCommandHandler : BaseCommandHandler<SendNotificationCommand>
{
    private readonly ICompositeNotificationService _notificationService;

    public SendNotificationCommandHandler(
        ICompositeNotificationService notificationService,
        ILogger<SendNotificationCommandHandler> logger) : base(logger)
    {
        _notificationService = notificationService;
    }

    protected override async Task HandleAsync(SendNotificationCommand command, ConsumeContext<SendNotificationCommand> context)
    {
        var success = await _notificationService.SendNotificationWithPreferencesAsync(
            command.TargetUserId,
            command.NotificationType,
            command.Title,
            command.Body,
            command.Data);

        if (!success)
        {
            Logger.LogWarning("Failed to send notification to user {UserId} with type {NotificationType}", 
                command.TargetUserId, command.NotificationType);
            
            // Don't throw exception for notification failures as they're not critical
            // Just log the failure and continue
        }
    }
}

/// <summary>
/// Handler for message notification commands
/// </summary>
public class SendMessageNotificationCommandHandler : BaseCommandHandler<SendMessageNotificationCommand>
{
    private readonly ICompositeNotificationService _notificationService;

    public SendMessageNotificationCommandHandler(
        ICompositeNotificationService notificationService,
        ILogger<SendMessageNotificationCommandHandler> logger) : base(logger)
    {
        _notificationService = notificationService;
    }

    protected override async Task HandleAsync(SendMessageNotificationCommand command, ConsumeContext<SendMessageNotificationCommand> context)
    {
        var success = await _notificationService.SendMessageNotificationAsync(
            command.TargetUserId,
            command.SenderUsername,
            command.MessageContent,
            command.ConversationId);

        if (!success)
        {
            Logger.LogWarning("Failed to send message notification to user {UserId} from {SenderUsername}", 
                command.TargetUserId, command.SenderUsername);
        }
    }
}

/// <summary>
/// Handler for post interaction notification commands
/// </summary>
public class SendPostInteractionNotificationCommandHandler : BaseCommandHandler<SendPostInteractionNotificationCommand>
{
    private readonly ICompositeNotificationService _notificationService;

    public SendPostInteractionNotificationCommandHandler(
        ICompositeNotificationService notificationService,
        ILogger<SendPostInteractionNotificationCommandHandler> logger) : base(logger)
    {
        _notificationService = notificationService;
    }

    protected override async Task HandleAsync(SendPostInteractionNotificationCommand command, ConsumeContext<SendPostInteractionNotificationCommand> context)
    {
        var title = command.InteractionType switch
        {
            "like" => "New Like",
            "comment" => "New Comment",
            "share" => "Post Shared",
            _ => "New Interaction"
        };

        var body = command.InteractionType switch
        {
            "like" => $"@{command.ActorUsername} liked your post",
            "comment" => $"@{command.ActorUsername} commented on your post" + 
                        (string.IsNullOrEmpty(command.CommentText) ? "" : $": {command.CommentText}"),
            "share" => $"@{command.ActorUsername} shared your post",
            _ => $"@{command.ActorUsername} interacted with your post"
        };

        var data = new Dictionary<string, string>
        {
            ["type"] = "post_interaction",
            ["postId"] = command.PostId.ToString(),
            ["interactionType"] = command.InteractionType,
            ["actorUsername"] = command.ActorUsername
        };

        var success = await _notificationService.SendNotificationWithPreferencesAsync(
            command.TargetUserId,
            "post_interaction",
            title,
            body,
            data);

        if (!success)
        {
            Logger.LogWarning("Failed to send post interaction notification to user {UserId} for post {PostId}", 
                command.TargetUserId, command.PostId);
        }
    }
}

/// <summary>
/// Handler for follow notification commands
/// </summary>
public class SendFollowNotificationCommandHandler : BaseCommandHandler<SendFollowNotificationCommand>
{
    private readonly ICompositeNotificationService _notificationService;

    public SendFollowNotificationCommandHandler(
        ICompositeNotificationService notificationService,
        ILogger<SendFollowNotificationCommandHandler> logger) : base(logger)
    {
        _notificationService = notificationService;
    }

    protected override async Task HandleAsync(SendFollowNotificationCommand command, ConsumeContext<SendFollowNotificationCommand> context)
    {
        var title = "New Follower";
        var body = $"@{command.FollowerUsername} started following you";
        var data = new Dictionary<string, string>
        {
            ["type"] = "follow",
            ["followerUsername"] = command.FollowerUsername
        };

        var success = await _notificationService.SendNotificationWithPreferencesAsync(
            command.TargetUserId,
            "follow",
            title,
            body,
            data);

        if (!success)
        {
            Logger.LogWarning("Failed to send follow notification to user {UserId} from {FollowerUsername}",
                command.TargetUserId, command.FollowerUsername);
        }
    }
}
