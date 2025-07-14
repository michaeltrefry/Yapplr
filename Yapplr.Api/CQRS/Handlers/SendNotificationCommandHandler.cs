using MassTransit;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Unified;

namespace Yapplr.Api.CQRS.Handlers;

/// <summary>
/// Handler for generic notification sending commands
/// </summary>
public class SendNotificationCommandHandler : BaseCommandHandler<SendNotificationCommand>
{
    private readonly IUnifiedNotificationService _notificationService;

    public SendNotificationCommandHandler(
        IUnifiedNotificationService notificationService,
        ILogger<SendNotificationCommandHandler> logger) : base(logger)
    {
        _notificationService = notificationService;
    }

    protected override async Task HandleAsync(SendNotificationCommand command, ConsumeContext<SendNotificationCommand> context)
    {
        var request = new NotificationRequest
        {
            UserId = command.TargetUserId,
            NotificationType = command.NotificationType,
            Title = command.Title,
            Body = command.Body,
            Data = command.Data
        };

        var success = await _notificationService.SendNotificationAsync(request);

        if (!success)
        {
            Logger.LogWarning("Failed to send notification to user {UserId} with type {NotificationType}", 
                command.TargetUserId, command.NotificationType);
            
            // Don't throw exception for notification failures as they're not critical
            // Just log the failure and continue
        }
    }
}