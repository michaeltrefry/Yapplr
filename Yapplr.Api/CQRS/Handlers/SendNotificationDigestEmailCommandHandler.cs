using MassTransit;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Services;
using Yapplr.Api.Services.EmailTemplates;

namespace Yapplr.Api.CQRS.Handlers;

/// <summary>
/// Handler for notification digest email commands
/// </summary>
public class SendNotificationDigestEmailCommandHandler : BaseCommandHandler<SendNotificationDigestEmailCommand>
{
    private readonly IEmailService _emailService;

    public SendNotificationDigestEmailCommandHandler(
        IEmailService emailService,
        ILogger<SendNotificationDigestEmailCommandHandler> logger) : base(logger)
    {
        _emailService = emailService;
    }

    protected override async Task HandleAsync(SendNotificationDigestEmailCommand command, ConsumeContext<SendNotificationDigestEmailCommand> context)
    {
        // Create notification digest email template
        var template = new NotificationDigestEmailTemplate(
            username: command.Username,
            notifications: command.Notifications,
            periodStart: command.PeriodStart,
            periodEnd: command.PeriodEnd,
            unsubscribeUrl: command.UnsubscribeUrl
        );

        var success = await _emailService.SendEmailAsync(
            command.ToEmail,
            template.Subject,
            template.GenerateHtmlBody(),
            template.GenerateTextBody()
        );

        if (!success)
        {
            Logger.LogWarning("Failed to send notification digest email to {ToEmail} for user {Username}",
                command.ToEmail, command.Username);
            throw new InvalidOperationException($"Failed to send notification digest email to {command.ToEmail}");
        }

        Logger.LogInformation("Sent notification digest email to {ToEmail} for user {Username} with {NotificationCount} notifications",
            command.ToEmail, command.Username, command.Notifications.Count);
    }
}
