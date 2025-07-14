using MassTransit;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Services;

namespace Yapplr.Api.CQRS.Handlers;

/// <summary>
/// Handler for generic email sending commands
/// </summary>
public class SendEmailCommandHandler : BaseCommandHandler<SendEmailCommand>
{
    private readonly IEmailService _emailService;

    public SendEmailCommandHandler(
        IEmailService emailService,
        ILogger<SendEmailCommandHandler> logger) : base(logger)
    {
        _emailService = emailService;
    }

    protected override async Task HandleAsync(SendEmailCommand command, ConsumeContext<SendEmailCommand> context)
    {
        var success = await _emailService.SendEmailAsync(
            command.ToEmail,
            command.Subject,
            command.HtmlBody,
            command.TextBody);

        if (!success)
        {
            Logger.LogWarning("Failed to send email to {ToEmail} with subject {Subject}", 
                command.ToEmail, command.Subject);
            
            // Let MassTransit handle retries based on configuration
            throw new InvalidOperationException($"Failed to send email to {command.ToEmail}");
        }
    }
}