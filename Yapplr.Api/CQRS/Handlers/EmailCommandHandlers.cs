using MassTransit;
using Microsoft.Extensions.Options;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Services;
using Yapplr.Api.Services.EmailTemplates;
using Yapplr.Api.Configuration;

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

/// <summary>
/// Handler for welcome email commands
/// </summary>
public class SendWelcomeEmailCommandHandler : BaseCommandHandler<SendWelcomeEmailCommand>
{
    private readonly IEmailService _emailService;
    private readonly FrontendUrlsConfiguration _frontendUrls;

    public SendWelcomeEmailCommandHandler(
        IEmailService emailService,
        ILogger<SendWelcomeEmailCommandHandler> logger,
        IOptions<FrontendUrlsConfiguration> frontendUrls) : base(logger)
    {
        _emailService = emailService;
        _frontendUrls = frontendUrls.Value;
    }

    protected override async Task HandleAsync(SendWelcomeEmailCommand command, ConsumeContext<SendWelcomeEmailCommand> context)
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
            Logger.LogWarning("Failed to send welcome email to {ToEmail} for user {Username}",
                command.ToEmail, command.Username);
            throw new InvalidOperationException($"Failed to send welcome email to {command.ToEmail}");
        }
    }
}

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

/// <summary>
/// Handler for password reset email commands
/// </summary>
public class SendPasswordResetEmailCommandHandler : BaseCommandHandler<SendPasswordResetEmailCommand>
{
    private readonly IEmailService _emailService;

    public SendPasswordResetEmailCommandHandler(
        IEmailService emailService,
        ILogger<SendPasswordResetEmailCommandHandler> logger) : base(logger)
    {
        _emailService = emailService;
    }

    protected override async Task HandleAsync(SendPasswordResetEmailCommand command, ConsumeContext<SendPasswordResetEmailCommand> context)
    {
        var subject = "Reset Your Password";
        var htmlBody = $@"
            <h1>Password Reset Request</h1>
            <p>Hi {command.Username},</p>
            <p>You requested to reset your password. Click the link below to create a new password:</p>
            <p><a href=""https://yapplr.com/reset-password?token={command.ResetToken}"">Reset Password</a></p>
            <p>This link will expire in 1 hour.</p>
            <p>If you didn't request this reset, please ignore this email and your password will remain unchanged.</p>";

        var textBody = $@"
            Password Reset Request

            Hi {command.Username},

            You requested to reset your password. Visit the link below to create a new password:
            https://yapplr.com/reset-password?token={command.ResetToken}

            This link will expire in 1 hour.

            If you didn't request this reset, please ignore this email and your password will remain unchanged.";

        var success = await _emailService.SendEmailAsync(command.ToEmail, subject, htmlBody, textBody);

        if (!success)
        {
            Logger.LogWarning("Failed to send password reset email to {ToEmail} for user {Username}",
                command.ToEmail, command.Username);
            throw new InvalidOperationException($"Failed to send password reset email to {command.ToEmail}");
        }
    }
}

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
