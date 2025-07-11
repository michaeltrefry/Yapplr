using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using System.Text;

namespace Yapplr.Api.Services;

public class AwsSesEmailService : IEmailService
{
    private readonly IAmazonSimpleEmailService _sesClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AwsSesEmailService> _logger;

    public AwsSesEmailService(
        IAmazonSimpleEmailService sesClient,
        IConfiguration configuration,
        ILogger<AwsSesEmailService> logger)
    {
        _sesClient = sesClient;
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
            // Check if SES client is available (might be null during migrations)
            if (_sesClient == null)
            {
                _logger.LogWarning("AWS SES client is not available - email sending skipped");
                return false;
            }

            var sesSettings = _configuration.GetSection("AwsSesSettings");
            var fromEmail = sesSettings["FromEmail"];
            var fromName = sesSettings["FromName"] ?? "Yapplr";

            if (string.IsNullOrEmpty(fromEmail))
            {
                _logger.LogError("AWS SES FromEmail not configured");
                return false;
            }

            var source = string.IsNullOrEmpty(fromName) ? fromEmail : $"{fromName} <{fromEmail}>";

            var sendRequest = new SendEmailRequest
            {
                Source = source,
                Destination = new Destination
                {
                    ToAddresses = new List<string> { toEmail }
                },
                Message = new Message
                {
                    Subject = new Content(subject),
                    Body = new Body()
                }
            };

            // Set HTML body
            if (!string.IsNullOrEmpty(htmlBody))
            {
                sendRequest.Message.Body.Html = new Content
                {
                    Charset = "UTF-8",
                    Data = htmlBody
                };
            }

            // Set text body
            if (!string.IsNullOrEmpty(textBody))
            {
                sendRequest.Message.Body.Text = new Content
                {
                    Charset = "UTF-8",
                    Data = textBody
                };
            }

            var response = await _sesClient.SendEmailAsync(sendRequest);
            
            _logger.LogInformation("Email sent successfully via AWS SES. MessageId: {MessageId}, To: {Email}", 
                response.MessageId, toEmail);
            
            return true;
        }
        catch (MessageRejectedException ex)
        {
            _logger.LogError(ex, "AWS SES rejected the email to {Email}: {Reason}", toEmail, ex.Message);
            return false;
        }
        catch (MailFromDomainNotVerifiedException ex)
        {
            _logger.LogError(ex, "AWS SES domain not verified for {Email}", toEmail);
            return false;
        }
        catch (ConfigurationSetDoesNotExistException ex)
        {
            _logger.LogError(ex, "AWS SES configuration set does not exist: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via AWS SES to {Email}", toEmail);
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
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ text-align: center; margin-bottom: 30px; padding: 20px 0; border-bottom: 2px solid #f0f0f0; }}
        .logo {{ font-size: 28px; font-weight: bold; color: #1d9bf0; margin-bottom: 10px; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 8px; margin: 20px 0; }}
        .button {{ display: inline-block; background: #1d9bf0; color: white; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-weight: bold; margin: 20px 0; }}
        .button:hover {{ background: #1a8cd8; }}
        .footer {{ text-align: center; margin-top: 30px; font-size: 14px; color: #666; padding-top: 20px; border-top: 1px solid #e0e0e0; }}
        .security-note {{ background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 4px; margin: 20px 0; }}
        .url-box {{ background: #f1f3f4; padding: 15px; border-radius: 4px; word-break: break-all; font-family: monospace; font-size: 14px; }}
        @media (max-width: 600px) {{
            .container {{ padding: 10px; }}
            .content {{ padding: 20px; }}
            .button {{ display: block; text-align: center; }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>Yapplr</div>
            <p style='margin: 0; color: #666;'>Social Media Platform</p>
        </div>
        
        <div class='content'>
            <h2 style='color: #333; margin-top: 0;'>Reset Your Password</h2>
            <p>Hi <strong>{username}</strong>,</p>
            <p>We received a request to reset your password for your Yapplr account. If you didn't make this request, you can safely ignore this email.</p>
            <p>To reset your password, click the button below:</p>
            <p style='text-align: center; margin: 30px 0;'>
                <a href='{resetUrl}' class='button'>Reset Password</a>
            </p>
            <p>Or copy and paste this link into your browser:</p>
            <div class='url-box'>{resetUrl}</div>
            
            <div class='security-note'>
                <strong>⚠️ Security Notice:</strong> This link will expire in 1 hour for security reasons. If you need a new reset link, please request another password reset.
            </div>
        </div>
        
        <div class='footer'>
            <p>This email was sent by Yapplr. If you have any questions, please contact our support team.</p>
            <p style='margin-top: 15px; font-size: 12px;'>© 2025 Yapplr. All rights reserved.</p>
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

SECURITY NOTICE: This link will expire in 1 hour for security reasons.

If you have any questions, please contact our support team.

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
        .tagline {{ font-size: 16px; margin: 10px 0 0 0; opacity: 0.9; }}
        .content {{ padding: 40px 30px; }}
        .welcome-badge {{ background: #e8f5e8; color: #28a745; padding: 15px 20px; border-radius: 8px; margin: 0 0 30px 0; text-align: center; border: 2px solid #28a745; }}
        .verification-code {{ font-size: 36px; font-weight: bold; color: #1d9bf0; text-align: center; background: #f8f9fa; padding: 25px; border-radius: 12px; letter-spacing: 6px; margin: 25px 0; border: 3px solid #1d9bf0; font-family: 'Courier New', monospace; }}
        .button {{ display: inline-block; background: linear-gradient(135deg, #1d9bf0 0%, #1a8cd8 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px; box-shadow: 0 4px 12px rgba(29, 155, 240, 0.3); }}
        .button:hover {{ background: linear-gradient(135deg, #1a8cd8 0%, #1976d2 100%); }}
        .security-note {{ background: #fff3cd; border: 1px solid #ffeaa7; color: #856404; padding: 15px; border-radius: 8px; margin: 25px 0; }}
        .footer {{ background: #f8f9fa; padding: 30px; text-align: center; color: #666; border-top: 1px solid #e9ecef; }}
        .social-links {{ margin: 20px 0; }}
        .social-links a {{ color: #1d9bf0; text-decoration: none; margin: 0 10px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 class='logo'>Yapplr</h1>
            <p class='tagline'>Connect. Share. Discover.</p>
        </div>

        <div class='content'>
            <div class='welcome-badge'>
                <strong>🎉 Welcome to Yapplr, {username}!</strong>
            </div>

            <h2 style='color: #333; margin-top: 0;'>Verify Your Email Address</h2>
            <p>Thanks for joining our community! To complete your registration and start connecting with others on Yapplr, please verify your email address.</p>

            <p><strong>Enter this verification code in the Yapplr app:</strong></p>
            <div class='verification-code'>{verificationToken}</div>

            <p>Or click the button below to verify your email instantly:</p>
            <p style='text-align: center; margin: 30px 0;'>
                <a href='{verificationUrl}' class='button'>Verify Email Address</a>
            </p>

            <div class='security-note'>
                <strong>⚠️ Security Notice:</strong> This verification code will expire in 24 hours. If you didn't create a Yapplr account, you can safely ignore this email.
            </div>
        </div>

        <div class='footer'>
            <p><strong>Welcome to Yapplr!</strong></p>
            <p>We're excited to have you join our growing community of creators, thinkers, and connectors.</p>
            <div class='social-links'>
                <a href='https://yapplr.com'>Visit Yapplr</a> |
                <a href='https://yapplr.com/support'>Get Support</a>
            </div>
            <p style='font-size: 12px; margin-top: 20px;'>© 2025 Yapplr. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateEmailVerificationText(string username, string verificationToken, string verificationUrl)
    {
        return $@"Welcome to Yapplr!

Hi {username},

Thanks for joining our community! To complete your registration and start connecting with others on Yapplr, please verify your email address.

Enter this verification code in the Yapplr app:

{verificationToken}

Or visit this link to verify your email:
{verificationUrl}

SECURITY NOTICE: This verification code will expire in 24 hours. If you didn't create a Yapplr account, you can safely ignore this email.

Welcome to Yapplr! We're excited to have you join our growing community of creators, thinkers, and connectors.

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
