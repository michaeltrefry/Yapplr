using MassTransit;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Unified;

namespace Yapplr.Api.CQRS.Handlers;

/// <summary>
/// Handler for follow notification commands
/// </summary>
public class SendFollowNotificationCommandHandler : BaseCommandHandler<SendFollowNotificationCommand>
{
    private readonly IUnifiedNotificationService _notificationService;

    public SendFollowNotificationCommandHandler(
        IUnifiedNotificationService notificationService,
        ILogger<SendFollowNotificationCommandHandler> logger) : base(logger)
    {
        _notificationService = notificationService;
    }

    protected override async Task HandleAsync(SendFollowNotificationCommand command, ConsumeContext<SendFollowNotificationCommand> context)
    {
        await _notificationService.SendFollowNotificationAsync(
            command.TargetUserId,
            command.FollowerUsername);

        Logger.LogInformation("Sent follow notification to user {UserId} from {FollowerUsername}",
            command.TargetUserId, command.FollowerUsername);
    }
}