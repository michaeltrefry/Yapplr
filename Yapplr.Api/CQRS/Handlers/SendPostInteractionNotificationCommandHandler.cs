using MassTransit;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Services;

namespace Yapplr.Api.CQRS.Handlers;

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