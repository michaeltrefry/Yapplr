using MassTransit;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Services;

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
        var htmlBody = $@"
            <h1>{command.Subject}</h1>
            <p>Hi {command.Username},</p>
            <p>{command.Message}</p>
            <p>Best regards,<br>The Yapplr Team</p>";

        var textBody = $@"
            {command.Subject}

            Hi {command.Username},

            {command.Message}

            Best regards,
            The Yapplr Team";

        var success = await _emailService.SendEmailAsync(command.ToEmail, command.Subject, htmlBody, textBody);

        if (!success)
        {
            Logger.LogWarning("Failed to send notification email to {ToEmail} for user {Username}",
                command.ToEmail, command.Username);
            throw new InvalidOperationException($"Failed to send notification email to {command.ToEmail}");
        }
    }
}