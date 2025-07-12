namespace Yapplr.Api.Services.EmailTemplates;

public class PasswordResetEmailTemplate : IEmailTemplate
{
    private readonly string _username;
    private readonly string _resetUrl;

    public PasswordResetEmailTemplate(string username, string resetUrl)
    {
        _username = username;
        _resetUrl = resetUrl;
    }

    public string Subject => "Reset Your Yapplr Password";

    public string GenerateHtmlBody()
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
        .reset-button {{ display: inline-block; background: linear-gradient(135deg, #1d9bf0 0%, #1a8cd8 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 8px; font-weight: bold; margin: 20px 0; }}
        .reset-button:hover {{ background: linear-gradient(135deg, #1a8cd8 0%, #1976d2 100%); }}
        .security-note {{ background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 6px; margin: 20px 0; }}
        .footer {{ background: #f8f9fa; padding: 30px; text-align: center; color: #666; }}
        .footer a {{ color: #1d9bf0; text-decoration: none; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 class='logo'>Yapplr</h1>
            <p style='margin: 10px 0 0 0; opacity: 0.9;'>Password Reset Request</p>
        </div>
        
        <div class='content'>
            <h2 style='color: #1d9bf0; margin-top: 0;'>Reset Your Password</h2>
            <p>Hi {_username},</p>
            <p>We received a request to reset your password for your Yapplr account. If you didn't make this request, you can safely ignore this email.</p>
            
            <p>To reset your password, click the button below:</p>
            
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{_resetUrl}' class='reset-button'>Reset My Password</a>
            </div>
            
            <p>Or copy and paste this link into your browser:</p>
            <p style='word-break: break-all; background: #f8f9fa; padding: 10px; border-radius: 4px; font-family: monospace;'>{_resetUrl}</p>

            <div class='security-note'>
                <strong>⚠️ Security Notice:</strong> This link will expire in 1 hour for security reasons.
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

    public string GenerateTextBody()
    {
        return $@"Reset Your Yapplr Password

Hi {_username},

We received a request to reset your password for your Yapplr account. If you didn't make this request, you can safely ignore this email.

To reset your password, visit this link:
{_resetUrl}

SECURITY NOTICE: This link will expire in 1 hour for security reasons.

If you have any questions, please contact our support team.

Best regards,
The Yapplr Team

© 2025 Yapplr. All rights reserved.";
    }
}
