using Yapplr.Api.Services.EmailTemplates;

namespace Yapplr.Api.Services;

public class UnifiedEmailService : IEmailService
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<UnifiedEmailService> _logger;

    public UnifiedEmailService(IEmailSender emailSender, ILogger<UnifiedEmailService> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string username, string resetToken, string resetUrl)
    {
        try
        {
            var template = new PasswordResetEmailTemplate(username, resetUrl);
            return await SendEmailAsync(toEmail, template.Subject, template.GenerateHtmlBody(), template.GenerateTextBody());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendEmailVerificationAsync(string toEmail, string username, string verificationToken, string verificationUrl)
    {
        try
        {
            var template = new EmailVerificationTemplate(username, verificationToken, verificationUrl);
            return await SendEmailAsync(toEmail, template.Subject, template.GenerateHtmlBody(), template.GenerateTextBody());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email verification to {Email}", toEmail);
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
