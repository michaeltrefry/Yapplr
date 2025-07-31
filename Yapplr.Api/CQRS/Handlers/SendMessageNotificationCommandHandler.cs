using MassTransit;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Services.Notifications;

namespace Yapplr.Api.CQRS.Handlers;

/// <summary>
/// Handler for message notification commands
/// </summary>
public class SendMessageNotificationCommandHandler : BaseCommandHandler<SendMessageNotificationCommand>
{
    private readonly INotificationService _notificationService;

    public SendMessageNotificationCommandHandler(
        INotificationService notificationService,
        ILogger<SendMessageNotificationCommandHandler> logger) : base(logger)
    {
        _notificationService = notificationService;
    }

    protected override async Task HandleAsync(SendMessageNotificationCommand command, ConsumeContext<SendMessageNotificationCommand> context)
    {
        await _notificationService.SendMessageNotificationAsync(
            command.TargetUserId,
            command.SenderUsername,
            command.MessageContent,
            command.ConversationId);

        Logger.LogInformation("Sent message notification to user {UserId} from {SenderUsername}",
            command.TargetUserId, command.SenderUsername);
    }
}