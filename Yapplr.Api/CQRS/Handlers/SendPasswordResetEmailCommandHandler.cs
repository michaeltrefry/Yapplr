using MassTransit;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Services;

namespace Yapplr.Api.CQRS.Handlers;

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