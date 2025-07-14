using MassTransit;
using Microsoft.Extensions.Options;
using Yapplr.Api.Configuration;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Services;
using Yapplr.Api.Services.EmailTemplates;

namespace Yapplr.Api.CQRS.Handlers;

/// <summary>
/// Handler for email verification commands
/// </summary>
public class SendEmailVerificationCommandHandler : BaseCommandHandler<SendEmailVerificationCommand>
{
    private readonly IEmailService _emailService;
    private readonly FrontendUrlsConfiguration _frontendUrls;

    public SendEmailVerificationCommandHandler(
        IEmailService emailService,
        ILogger<SendEmailVerificationCommandHandler> logger,
        IOptions<FrontendUrlsConfiguration> frontendUrls) : base(logger)
    {
        _emailService = emailService;
        _frontendUrls = frontendUrls.Value;
    }

    protected override async Task HandleAsync(SendEmailVerificationCommand command, ConsumeContext<SendEmailVerificationCommand> context)
    {
        // Use the proper email verification template with environment-aware URL
        var verificationUrl = _frontendUrls.GetVerifyEmailUrl(command.VerificationToken);
        var template = new EmailVerificationTemplate(command.Username, command.VerificationToken, verificationUrl);

        var success = await _emailService.SendEmailAsync(
            command.ToEmail,
            template.Subject,
            template.GenerateHtmlBody(),
            template.GenerateTextBody());

        if (!success)
        {
            Logger.LogWarning("Failed to send email verification to {ToEmail} for user {Username}",
                command.ToEmail, command.Username);
            throw new InvalidOperationException($"Failed to send email verification to {command.ToEmail}");
        }
    }
}