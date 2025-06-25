using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using System.Text;

namespace Postr.Api.Services;

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
        var subject = "Reset Your Postr Password";
        var htmlBody = GeneratePasswordResetHtml(username, resetUrl);
        var textBody = GeneratePasswordResetText(username, resetUrl);

        return await SendEmailAsync(toEmail, subject, htmlBody, textBody);
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null)
    {
        try
        {
            var sesSettings = _configuration.GetSection("AwsSesSettings");
            var fromEmail = sesSettings["FromEmail"];
            var fromName = sesSettings["FromName"] ?? "Postr";

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
    <title>Reset Your Postr Password</title>
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
            <div class='logo'>Postr</div>
            <p style='margin: 0; color: #666;'>Social Media Platform</p>
        </div>
        
        <div class='content'>
            <h2 style='color: #333; margin-top: 0;'>Reset Your Password</h2>
            <p>Hi <strong>{username}</strong>,</p>
            <p>We received a request to reset your password for your Postr account. If you didn't make this request, you can safely ignore this email.</p>
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
            <p>This email was sent by Postr. If you have any questions, please contact our support team.</p>
            <p style='margin-top: 15px; font-size: 12px;'>© 2025 Postr. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GeneratePasswordResetText(string username, string resetUrl)
    {
        return $@"Reset Your Postr Password

Hi {username},

We received a request to reset your password for your Postr account. If you didn't make this request, you can safely ignore this email.

To reset your password, visit this link:
{resetUrl}

SECURITY NOTICE: This link will expire in 1 hour for security reasons.

If you have any questions, please contact our support team.

Best regards,
The Postr Team

© 2025 Postr. All rights reserved.";
    }
}
