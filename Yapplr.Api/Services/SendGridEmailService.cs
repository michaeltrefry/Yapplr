using SendGrid;
using SendGrid.Helpers.Mail;

namespace Yapplr.Api.Services;

public class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _sendGridClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(
        ISendGridClient sendGridClient,
        IConfiguration configuration,
        ILogger<SendGridEmailService> logger)
    {
        _sendGridClient = sendGridClient;
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
            var sendGridSettings = _configuration.GetSection("SendGridSettings");
            var fromEmail = sendGridSettings["FromEmail"];
            var fromName = sendGridSettings["FromName"] ?? "Yapplr";

            if (string.IsNullOrEmpty(fromEmail))
            {
                _logger.LogError("SendGrid FromEmail is not configured");
                return false;
            }

            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(toEmail);

            var msg = MailHelper.CreateSingleEmail(from, to, subject, textBody ?? htmlBody, htmlBody);

            var response = await _sendGridClient.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
                return true;
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                _logger.LogError("SendGrid failed to send email to {ToEmail}. Status: {StatusCode}, Response: {Response}", 
                    toEmail, response.StatusCode, responseBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendGrid rejected the email to {ToEmail}: {Message}", toEmail, ex.Message);
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
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; }}
        .header {{ background: linear-gradient(135deg, #1d9bf0 0%, #1a8cd8 100%); color: white; padding: 40px 20px; text-align: center; }}
        .logo {{ font-size: 28px; font-weight: bold; margin: 0; }}
        .content {{ padding: 40px 30px; }}
        .button {{ display: inline-block; background: linear-gradient(135deg, #1d9bf0 0%, #1a8cd8 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px; }}
        .footer {{ background: #f8f9fa; padding: 30px; text-align: center; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 class='logo'>Yapplr</h1>
        </div>
        <div class='content'>
            <h2>Reset Your Password</h2>
            <p>Hi {username},</p>
            <p>We received a request to reset your Yapplr password. Click the button below to create a new password:</p>
            <p style='text-align: center; margin: 30px 0;'>
                <a href='{resetUrl}' class='button'>Reset Password</a>
            </p>
            <p>If you didn't request this password reset, you can safely ignore this email.</p>
            <p>This link will expire in 1 hour for security reasons.</p>
        </div>
        <div class='footer'>
            <p>© 2025 Yapplr. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GeneratePasswordResetText(string username, string resetUrl)
    {
        return $@"Reset Your Yapplr Password

Hi {username},

We received a request to reset your Yapplr password. Visit the following link to create a new password:

{resetUrl}

If you didn't request this password reset, you can safely ignore this email.

This link will expire in 1 hour for security reasons.

Best regards,
The Yapplr Team

© 2025 Yapplr. All rights reserved.";
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
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; }}
        .header {{ background: linear-gradient(135deg, #1d9bf0 0%, #1a8cd8 100%); color: white; padding: 40px 20px; text-align: center; }}
        .logo {{ font-size: 28px; font-weight: bold; margin: 0; }}
        .content {{ padding: 40px 30px; }}
        .verification-code {{ font-size: 36px; font-weight: bold; color: #1d9bf0; text-align: center; background: #f8f9fa; padding: 25px; border-radius: 12px; letter-spacing: 6px; margin: 25px 0; border: 3px solid #1d9bf0; font-family: 'Courier New', monospace; }}
        .button {{ display: inline-block; background: linear-gradient(135deg, #1d9bf0 0%, #1a8cd8 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px; }}
        .welcome-badge {{ background: #e8f5e8; color: #28a745; padding: 15px 20px; border-radius: 8px; margin: 0 0 30px 0; text-align: center; border: 2px solid #28a745; }}
        .footer {{ background: #f8f9fa; padding: 30px; text-align: center; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 class='logo'>Yapplr</h1>
        </div>
        <div class='content'>
            <div class='welcome-badge'>
                <strong>🎉 Welcome to Yapplr, {username}!</strong>
            </div>
            <h2>Verify Your Email Address</h2>
            <p>Thanks for joining our community! To complete your registration, please verify your email address.</p>
            <p><strong>Enter this verification code in the Yapplr app:</strong></p>
            <div class='verification-code'>{verificationToken}</div>
            <p>Or click the button below to verify your email instantly:</p>
            <p style='text-align: center; margin: 30px 0;'>
                <a href='{verificationUrl}' class='button'>Verify Email Address</a>
            </p>
            <p><strong>This verification code will expire in 24 hours.</strong></p>
        </div>
        <div class='footer'>
            <p>Welcome to Yapplr! We're excited to have you join our community.</p>
            <p>© 2025 Yapplr. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateEmailVerificationText(string username, string verificationToken, string verificationUrl)
    {
        return $@"Welcome to Yapplr!

Hi {username},

Thanks for joining our community! To complete your registration, please verify your email address.

Enter this verification code in the Yapplr app:

{verificationToken}

Or visit this link to verify your email:
{verificationUrl}

This verification code will expire in 24 hours.

Welcome to Yapplr! We're excited to have you join our community.

Best regards,
The Yapplr Team

© 2025 Yapplr. All rights reserved.";
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
