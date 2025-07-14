using MassTransit;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Services;

namespace Yapplr.Api.CQRS.Handlers;

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