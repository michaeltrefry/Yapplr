using MassTransit;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Services;

namespace Yapplr.Api.CQRS.Handlers;

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