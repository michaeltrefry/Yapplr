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

    public async Task<bool> SendUserSuspensionEmailAsync(string toEmail, string username, string reason, DateTime? suspendedUntil, string moderatorUsername, string appealUrl)
    {
        var subject = "Important: Your Yapplr Account Has Been Suspended";
        var htmlBody = GenerateUserSuspensionHtml(username, reason, suspendedUntil, moderatorUsername, appealUrl);
        var textBody = GenerateUserSuspensionText(username, reason, suspendedUntil, moderatorUsername, appealUrl);

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

    private string GenerateUserSuspensionHtml(string username, string reason, DateTime? suspendedUntil, string moderatorUsername, string appealUrl)
    {
        var suspensionDuration = suspendedUntil.HasValue
            ? $"until {suspendedUntil.Value:MMMM dd, yyyy 'at' HH:mm} UTC"
            : "indefinitely";

        var durationMessage = suspendedUntil.HasValue
            ? $"<p><strong>Suspension End Date:</strong> {suspendedUntil.Value:MMMM dd, yyyy 'at' HH:mm} UTC</p>"
            : "<p><strong>Duration:</strong> Indefinite (requires manual review)</p>";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Account Suspension Notice - Yapplr</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .logo {{ font-size: 24px; font-weight: bold; color: #1d9bf0; }}
        .content {{ background: #fff8f0; padding: 30px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #f59e0b; }}
        .warning-box {{ background: #fef3cd; border: 1px solid #f59e0b; padding: 20px; border-radius: 6px; margin: 20px 0; }}
        .info-box {{ background: #f0f9ff; border: 1px solid #0ea5e9; padding: 20px; border-radius: 6px; margin: 20px 0; }}
        .button {{ display: inline-block; background: #1d9bf0; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; text-align: center; }}
        .appeal-button {{ background: #059669; }}
        .footer {{ text-align: center; margin-top: 30px; font-size: 14px; color: #666; }}
        .reason-box {{ background: #f9fafb; border: 1px solid #d1d5db; padding: 15px; border-radius: 6px; margin: 15px 0; }}
        ul {{ padding-left: 20px; }}
        li {{ margin: 8px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>Yapplr</div>
        </div>

        <div class='content'>
            <h2 style='color: #f59e0b; margin-top: 0;'>⚠️ Account Suspension Notice</h2>
            <p>Hello {username},</p>
            <p>We are writing to inform you that your Yapplr account has been suspended {suspensionDuration}.</p>

            <div class='warning-box'>
                <h3 style='margin-top: 0; color: #92400e;'>Suspension Details</h3>
                <p><strong>Reason:</strong></p>
                <div class='reason-box'>{reason}</div>
                <p><strong>Moderator:</strong> @{moderatorUsername}</p>
                {durationMessage}
            </div>

            <div class='info-box'>
                <h3 style='margin-top: 0; color: #0c4a6e;'>What This Means</h3>
                <ul>
                    <li>You cannot post, comment, or interact with content</li>
                    <li>Your existing content remains visible to other users</li>
                    <li>You can still view content but cannot participate</li>
                    <li>You will not receive notifications during suspension</li>
                </ul>
            </div>

            <h3>Think This Was a Mistake?</h3>
            <p>If you believe this suspension was issued in error, you can submit an appeal for review by our moderation team.</p>
            <p style='text-align: center; margin: 30px 0;'>
                <a href='{appealUrl}' class='button appeal-button'>Submit an Appeal</a>
            </p>

            <p><strong>Important:</strong> Please review our <a href='https://yapplr.com/community-guidelines'>Community Guidelines</a> before your suspension ends to ensure future compliance.</p>
        </div>

        <div class='footer'>
            <p>This is an automated message from the Yapplr Moderation Team.</p>
            <p>If you have questions about this suspension, please use the appeal process above.</p>
            <p>&copy; 2024 Yapplr. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateUserSuspensionText(string username, string reason, DateTime? suspendedUntil, string moderatorUsername, string appealUrl)
    {
        var suspensionDuration = suspendedUntil.HasValue
            ? $"until {suspendedUntil.Value:MMMM dd, yyyy 'at' HH:mm} UTC"
            : "indefinitely";

        var durationMessage = suspendedUntil.HasValue
            ? $"Suspension End Date: {suspendedUntil.Value:MMMM dd, yyyy 'at' HH:mm} UTC"
            : "Duration: Indefinite (requires manual review)";

        return $@"ACCOUNT SUSPENSION NOTICE - YAPPLR

Hello {username},

We are writing to inform you that your Yapplr account has been suspended {suspensionDuration}.

SUSPENSION DETAILS:
Reason: {reason}
Moderator: @{moderatorUsername}
{durationMessage}

WHAT THIS MEANS:
• You cannot post, comment, or interact with content
• Your existing content remains visible to other users
• You can still view content but cannot participate
• You will not receive notifications during suspension

THINK THIS WAS A MISTAKE?
If you believe this suspension was issued in error, you can submit an appeal for review by our moderation team.

Submit an Appeal: {appealUrl}

IMPORTANT: Please review our Community Guidelines at https://yapplr.com/community-guidelines before your suspension ends to ensure future compliance.

This is an automated message from the Yapplr Moderation Team.
If you have questions about this suspension, please use the appeal process above.

© 2024 Yapplr. All rights reserved.";
    }
}
