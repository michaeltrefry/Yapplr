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
        var htmlBody = GeneratePasswordResetHtml(username, resetToken, resetUrl);
        var textBody = GeneratePasswordResetText(username, resetToken, resetUrl);

        return await SendEmailAsync(toEmail, subject, htmlBody, textBody);
    }

    public async Task<bool> SendEmailVerificationAsync(string toEmail, string username, string verificationToken, string verificationUrl)
    {
        var subject = "Verify Your Yapplr Email Address";
        var htmlBody = GenerateEmailVerificationHtml(username, verificationToken, verificationUrl);
        var textBody = GenerateEmailVerificationText(username, verificationToken, verificationUrl);

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

    private string GeneratePasswordResetHtml(string username, string resetToken, string resetUrl)
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
        .code {{ font-size: 32px; font-weight: bold; color: #1d9bf0; text-align: center; background: white; padding: 20px; border-radius: 8px; letter-spacing: 4px; margin: 20px 0; border: 2px solid #1d9bf0; }}
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
            <p>Enter this 6-digit code in the Yapplr app to reset your password:</p>
            <div class='code'>{resetToken}</div>
            <p>Alternatively, you can click the button below to reset your password on the web:</p>
            <p style='text-align: center; margin: 30px 0;'>
                <a href='{resetUrl}' class='button'>Reset Password on Web</a>
            </p>
            <p><strong>This code will expire in 1 hour for security reasons.</strong></p>
        </div>
        
        <div class='footer'>
            <p>This email was sent by Yapplr. If you have any questions, please contact our support team.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GeneratePasswordResetText(string username, string resetToken, string resetUrl)
    {
        return $@"Reset Your Yapplr Password

Hi {username},

We received a request to reset your password for your Yapplr account. If you didn't make this request, you can safely ignore this email.

Enter this 6-digit code in the Yapplr app to reset your password:

{resetToken}

Alternatively, you can visit this link to reset your password on the web:
{resetUrl}

This code will expire in 1 hour for security reasons.

If you have any questions, please contact our support team.

Best regards,
The Yapplr Team";
    }

    private string GenerateEmailVerificationHtml(string username, string verificationToken, string verificationUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Verify Your Yapplr Email</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .logo {{ font-size: 24px; font-weight: bold; color: #1d9bf0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 8px; margin: 20px 0; }}
        .button {{ display: inline-block; background: #1d9bf0; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; }}
        .code {{ font-size: 32px; font-weight: bold; color: #1d9bf0; text-align: center; background: white; padding: 20px; border-radius: 8px; letter-spacing: 4px; margin: 20px 0; border: 2px solid #1d9bf0; }}
        .footer {{ text-align: center; margin-top: 30px; font-size: 14px; color: #666; }}
        .welcome {{ background: #e8f5e8; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #28a745; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>Yapplr</div>
        </div>

        <div class='welcome'>
            <h2 style='color: #28a745; margin-top: 0;'>Welcome to Yapplr! 🎉</h2>
            <p>Thanks for joining our community, <strong>{username}</strong>!</p>
        </div>

        <div class='content'>
            <h2>Verify Your Email Address</h2>
            <p>To complete your registration and start using Yapplr, please verify your email address by entering this 6-digit code in the app:</p>
            <div class='code'>{verificationToken}</div>
            <p>Alternatively, you can click the button below to verify your email on the web:</p>
            <p style='text-align: center; margin: 30px 0;'>
                <a href='{verificationUrl}' class='button'>Verify Email Address</a>
            </p>
            <p><strong>This verification code will expire in 24 hours for security reasons.</strong></p>
            <p>If you didn't create a Yapplr account, you can safely ignore this email.</p>
        </div>

        <div class='footer'>
            <p>Welcome to Yapplr! We're excited to have you join our community.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateEmailVerificationText(string username, string verificationToken, string verificationUrl)
    {
        return $@"Welcome to Yapplr!

Hi {username},

Thanks for joining our community! To complete your registration and start using Yapplr, please verify your email address.

Enter this 6-digit code in the Yapplr app to verify your email:

{verificationToken}

Alternatively, you can visit this link to verify your email on the web:
{verificationUrl}

This verification code will expire in 24 hours for security reasons.

If you didn't create a Yapplr account, you can safely ignore this email.

Welcome to Yapplr! We're excited to have you join our community.

Best regards,
The Yapplr Team";
    }
}
