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