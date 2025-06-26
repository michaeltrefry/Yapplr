using System.Net;
using System.Net.Mail;
using System.Text;

namespace Yapplr.Api.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string username, string resetToken, string resetUrl)
    {
        var subject = "Reset Your Yapplr Password";
        var htmlBody = GeneratePasswordResetHtml(username, resetUrl);
        var textBody = GeneratePasswordResetText(username, resetUrl);

        return await SendEmailAsync(toEmail, subject, htmlBody, textBody);
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null)
    {
        try
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var host = smtpSettings["Host"];
            var port = int.Parse(smtpSettings["Port"] ?? "587");
            var username = smtpSettings["Username"];
            var password = smtpSettings["Password"];
            var fromEmail = smtpSettings["FromEmail"];
            var fromName = smtpSettings["FromName"] ?? "Yapplr";

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("SMTP settings not configured. Email not sent.");
                return false;
            }

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail ?? username, fromName),
                Subject = subject,
                IsBodyHtml = true,
                Body = htmlBody
            };

            message.To.Add(toEmail);

            if (!string.IsNullOrEmpty(textBody))
            {
                var textView = AlternateView.CreateAlternateViewFromString(textBody, Encoding.UTF8, "text/plain");
                message.AlternateViews.Add(textView);
            }

            await client.SendMailAsync(message);
            _logger.LogInformation("Password reset email sent successfully to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            return false;
        }
    }

    private string GeneratePasswordResetHtml(string username, string resetUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Reset Your Yapplr Password</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .logo {{ font-size: 24px; font-weight: bold; color: #1d9bf0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 8px; margin: 20px 0; }}
        .button {{ display: inline-block; background: #1d9bf0; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; }}
        .footer {{ text-align: center; margin-top: 30px; font-size: 14px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>Yapplr</div>
        </div>
        
        <div class='content'>
            <h2>Reset Your Password</h2>
            <p>Hi {username},</p>
            <p>We received a request to reset your password for your Yapplr account. If you didn't make this request, you can safely ignore this email.</p>
            <p>To reset your password, click the button below:</p>
            <p style='text-align: center; margin: 30px 0;'>
                <a href='{resetUrl}' class='button'>Reset Password</a>
            </p>
            <p>Or copy and paste this link into your browser:</p>
            <p style='word-break: break-all; color: #666;'>{resetUrl}</p>
            <p><strong>This link will expire in 1 hour for security reasons.</strong></p>
        </div>
        
        <div class='footer'>
            <p>This email was sent by Yapplr. If you have any questions, please contact our support team.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GeneratePasswordResetText(string username, string resetUrl)
    {
        return $@"Reset Your Yapplr Password

Hi {username},

We received a request to reset your password for your Yapplr account. If you didn't make this request, you can safely ignore this email.

To reset your password, visit this link:
{resetUrl}

This link will expire in 1 hour for security reasons.

If you have any questions, please contact our support team.

Best regards,
The Yapplr Team";
    }
}
