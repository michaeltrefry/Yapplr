namespace Yapplr.Api.Services;

public interface IEmailService
{
    Task<bool> SendPasswordResetEmailAsync(string toEmail, string username, string resetToken, string resetUrl);
    Task<bool> SendEmailVerificationAsync(string toEmail, string username, string verificationToken, string verificationUrl);
    Task<bool> SendUserSuspensionEmailAsync(string toEmail, string username, string reason, DateTime? suspendedUntil, string moderatorUsername, string appealUrl);
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null);
}