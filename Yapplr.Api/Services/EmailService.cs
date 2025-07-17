using Yapplr.Api.Services.EmailTemplates;
using Serilog.Context;

namespace Yapplr.Api.Services;

public class EmailService : IEmailService
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IEmailSender emailSender, ILogger<EmailService> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string username, string resetToken, string resetUrl)
    {
        using var operationScope = LogContext.PushProperty("Operation", "SendPasswordResetEmail");
        using var emailScope = LogContext.PushProperty("ToEmail", toEmail);
        using var usernameScope = LogContext.PushProperty("Username", username);
        using var emailTypeScope = LogContext.PushProperty("EmailType", "PasswordReset");

        _logger.LogInformation("Sending password reset email to {ToEmail} for user {Username}", toEmail, username);

        try
        {
            var template = new PasswordResetEmailTemplate(username, resetUrl);
            var result = await SendEmailAsync(toEmail, template.Subject, template.GenerateHtmlBody(), template.GenerateTextBody());

            if (result)
            {
                _logger.LogInformation("Password reset email sent successfully to {ToEmail}", toEmail);
            }
            else
            {
                _logger.LogWarning("Password reset email failed to send to {ToEmail}", toEmail);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {ToEmail}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendEmailVerificationAsync(string toEmail, string username, string verificationToken, string verificationUrl)
    {
        using var operationScope = LogContext.PushProperty("Operation", "SendEmailVerification");
        using var emailScope = LogContext.PushProperty("ToEmail", toEmail);
        using var usernameScope = LogContext.PushProperty("Username", username);
        using var emailTypeScope = LogContext.PushProperty("EmailType", "EmailVerification");

        _logger.LogInformation("Sending email verification to {ToEmail} for user {Username}", toEmail, username);

        try
        {
            var template = new EmailVerificationTemplate(username, verificationToken, verificationUrl);
            var result = await SendEmailAsync(toEmail, template.Subject, template.GenerateHtmlBody(), template.GenerateTextBody());

            if (result)
            {
                _logger.LogInformation("Email verification sent successfully to {ToEmail}", toEmail);
            }
            else
            {
                _logger.LogWarning("Email verification failed to send to {ToEmail}", toEmail);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email verification to {ToEmail}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendUserSuspensionEmailAsync(string toEmail, string username, string reason, DateTime? suspendedUntil, string moderatorUsername, string appealUrl)
    {
        try
        {
            var template = new UserSuspensionEmailTemplate(username, reason, suspendedUntil, moderatorUsername, appealUrl);
            return await SendEmailAsync(toEmail, template.Subject, template.GenerateHtmlBody(), template.GenerateTextBody());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send user suspension email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null)
    {
        try
        {
            return await _emailSender.SendEmailAsync(toEmail, subject, htmlBody, textBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            return false;
        }
    }
}
