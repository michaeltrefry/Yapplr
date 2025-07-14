using MassTransit;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Services.Unified;

namespace Yapplr.Api.CQRS.Handlers;

/// <summary>
/// Handler for post interaction notification commands
/// </summary>
public class SendPostInteractionNotificationCommandHandler : BaseCommandHandler<SendPostInteractionNotificationCommand>
{
    private readonly IUnifiedNotificationService _notificationService;

    public SendPostInteractionNotificationCommandHandler(
        IUnifiedNotificationService notificationService,
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

        // Use specific notification methods based on interaction type
        switch (command.InteractionType.ToLower())
        {
            case "like":
                await _notificationService.SendLikeNotificationAsync(
                    command.TargetUserId,
                    command.ActorUsername,
                    command.PostId);
                break;
            case "comment":
                await _notificationService.SendCommentNotificationAsync(
                    command.TargetUserId,
                    command.ActorUsername,
                    command.PostId,
                    0); // Comment ID would need to be passed in the command
                break;
            case "share":
                await _notificationService.SendRepostNotificationAsync(
                    command.TargetUserId,
                    command.ActorUsername,
                    command.PostId);
                break;
            default:
                // For unknown interaction types, use the generic notification method
                var request = new NotificationRequest
                {
                    UserId = command.TargetUserId,
                    NotificationType = "post_interaction",
                    Title = $"New {command.InteractionType}",
                    Body = $"@{command.ActorUsername} {command.InteractionType}d your post",
                    Data = new Dictionary<string, string>
                    {
                        ["type"] = "post_interaction",
                        ["postId"] = command.PostId.ToString(),
                        ["interactionType"] = command.InteractionType,
                        ["actorUsername"] = command.ActorUsername
                    }
                };
                await _notificationService.SendNotificationAsync(request);
                break;
        }

        Logger.LogInformation("Sent {InteractionType} notification to user {UserId} for post {PostId}",
            command.InteractionType, command.TargetUserId, command.PostId);
    }
}