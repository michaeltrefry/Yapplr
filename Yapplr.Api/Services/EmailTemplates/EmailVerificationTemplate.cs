namespace Yapplr.Api.Services.EmailTemplates;

public class EmailVerificationTemplate : IEmailTemplate
{
    private readonly string _username;
    private readonly string _verificationToken;
    private readonly string _verificationUrl;

    public EmailVerificationTemplate(string username, string verificationToken, string verificationUrl)
    {
        _username = username;
        _verificationToken = verificationToken;
        _verificationUrl = verificationUrl;
    }

    public string Subject => "Verify Your Yapplr Email Address";

    public string GenerateHtmlBody()
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
        .verify-button {{ display: inline-block; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 8px; font-weight: bold; margin: 20px 0; }}
        .verify-button:hover {{ background: linear-gradient(135deg, #059669 0%, #047857 100%); }}
        .verification-code {{ background: #f0f9ff; border: 2px dashed #1d9bf0; padding: 20px; text-align: center; margin: 20px 0; border-radius: 8px; }}
        .code {{ font-size: 24px; font-weight: bold; color: #1d9bf0; letter-spacing: 2px; font-family: monospace; }}
        .security-note {{ background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 6px; margin: 20px 0; }}
        .footer {{ background: #f8f9fa; padding: 30px; text-align: center; color: #666; }}
        .footer a {{ color: #1d9bf0; text-decoration: none; }}
        .social-links {{ margin: 15px 0; }}
        .social-links a {{ margin: 0 10px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 class='logo'>Yapplr</h1>
            <p style='margin: 10px 0 0 0; opacity: 0.9;'>Welcome to the Community!</p>
        </div>
        
        <div class='content'>
            <h2 style='color: #10b981; margin-top: 0;'>🎉 Welcome to Yapplr!</h2>
            <p>Hi {_username},</p>
            <p>Thanks for joining our community! To complete your registration and start connecting with others on Yapplr, please verify your email address.</p>
            
            <div class='verification-code'>
                <p style='margin: 0 0 10px 0; font-weight: bold;'>Your Verification Code:</p>
                <div class='code'>{_verificationToken}</div>
                <p style='margin: 10px 0 0 0; font-size: 14px; color: #666;'>Enter this code in the Yapplr app</p>
            </div>
            
            <p>Or click the button below to verify automatically:</p>
            
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{_verificationUrl}' class='verify-button'>Verify My Email</a>
            </div>

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

    public string GenerateTextBody()
    {
        return $@"Welcome to Yapplr!

Hi {_username},

Thanks for joining our community! To complete your registration and start connecting with others on Yapplr, please verify your email address.

Enter this verification code in the Yapplr app:

{_verificationToken}

Or visit this link to verify your email:
{_verificationUrl}

SECURITY NOTICE: This verification code will expire in 24 hours. If you didn't create a Yapplr account, you can safely ignore this email.

Welcome to Yapplr! We're excited to have you join our growing community of creators, thinkers, and connectors.

Best regards,
The Yapplr Team

© 2025 Yapplr. All rights reserved.";
    }
}
