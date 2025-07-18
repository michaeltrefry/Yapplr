using MassTransit;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Services;
using Yapplr.Api.Services.EmailTemplates;

namespace Yapplr.Api.CQRS.Handlers;

/// <summary>
/// Handler for notification email commands
/// </summary>
public class SendNotificationEmailCommandHandler : BaseCommandHandler<SendNotificationEmailCommand>
{
    private readonly IEmailService _emailService;

    public SendNotificationEmailCommandHandler(
        IEmailService emailService,
        ILogger<SendNotificationEmailCommandHandler> logger) : base(logger)
    {
        _emailService = emailService;
    }

    protected override async Task HandleAsync(SendNotificationEmailCommand command, ConsumeContext<SendNotificationEmailCommand> context)
    {
        // Create notification email template
        var template = new NotificationEmailTemplate(
            username: command.Username,
            notificationTitle: command.Subject,
            notificationBody: command.Message,
            notificationType: command.NotificationType ?? "notification",
            actionUrl: command.ActionUrl
        );

        var success = await _emailService.SendEmailAsync(
            command.ToEmail,
            template.Subject,
            template.GenerateHtmlBody(),
            template.GenerateTextBody()
        );

        if (!success)
        {
            Logger.LogWarning("Failed to send notification email to {ToEmail} for user {Username}",
                command.ToEmail, command.Username);
            throw new InvalidOperationException($"Failed to send notification email to {command.ToEmail}");
        }
    }
}