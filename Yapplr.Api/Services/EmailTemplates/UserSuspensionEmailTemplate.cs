namespace Yapplr.Api.Services.EmailTemplates;

public class UserSuspensionEmailTemplate : IEmailTemplate
{
    private readonly string _username;
    private readonly string _reason;
    private readonly DateTime? _suspendedUntil;
    private readonly string _moderatorUsername;
    private readonly string _appealUrl;

    public UserSuspensionEmailTemplate(string username, string reason, DateTime? suspendedUntil, string moderatorUsername, string appealUrl)
    {
        _username = username;
        _reason = reason;
        _suspendedUntil = suspendedUntil;
        _moderatorUsername = moderatorUsername;
        _appealUrl = appealUrl;
    }

    public string Subject => "Important: Your Yapplr Account Has Been Suspended";

    public string GenerateHtmlBody()
    {
        var suspensionDuration = _suspendedUntil.HasValue ? "temporarily" : "indefinitely";
        var durationMessage = _suspendedUntil.HasValue 
            ? $"<p><strong>Suspension End:</strong> {_suspendedUntil.Value:MMMM dd, yyyy 'at' h:mm tt} UTC</p>"
            : "<p><strong>Duration:</strong> Indefinite (requires appeal for review)</p>";

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
        .reason-box {{ background: #f9fafb; border: 1px solid #d1d5db; padding: 15px; border-radius: 4px; margin: 10px 0; font-style: italic; }}
        .appeal-button {{ display: inline-block; background: #dc2626; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; margin: 15px 0; }}
        .appeal-button:hover {{ background: #b91c1c; }}
        .footer {{ text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #e5e7eb; color: #666; }}
        .guidelines-link {{ color: #1d9bf0; text-decoration: none; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>Yapplr</div>
        </div>

        <div class='content'>
            <h2 style='color: #f59e0b; margin-top: 0;'>⚠️ Account Suspension Notice</h2>
            <p>Hello {_username},</p>
            <p>We are writing to inform you that your Yapplr account has been suspended {suspensionDuration}.</p>

            <div class='warning-box'>
                <h3 style='margin-top: 0; color: #92400e;'>Suspension Details</h3>
                <p><strong>Reason:</strong></p>
                <div class='reason-box'>{_reason}</div>
                <p><strong>Moderator:</strong> @{_moderatorUsername}</p>
                {durationMessage}
            </div>

            <h3 style='color: #dc2626;'>What This Means</h3>
            <ul>
                <li>You cannot post, comment, or interact with content</li>
                <li>Your existing content remains visible to other users</li>
                <li>You can still view content but cannot participate</li>
                <li>You will not receive notifications during suspension</li>
            </ul>

            <h3 style='color: #059669;'>Think This Was a Mistake?</h3>
            <p>If you believe this suspension was issued in error, you can submit an appeal for review by our moderation team.</p>
            
            <div style='text-align: center;'>
                <a href='{_appealUrl}' class='appeal-button'>Submit an Appeal</a>
            </div>
        </div>

        <div class='footer'>
            <p><strong>Important:</strong> Please review our <a href='https://yapplr.com/community-guidelines' class='guidelines-link'>Community Guidelines</a> before your suspension ends to ensure future compliance.</p>
            <p style='margin-top: 15px; font-size: 12px;'>© 2025 Yapplr. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    public string GenerateTextBody()
    {
        var suspensionDuration = _suspendedUntil.HasValue ? "temporarily" : "indefinitely";
        var durationMessage = _suspendedUntil.HasValue 
            ? $"Suspension End: {_suspendedUntil.Value:MMMM dd, yyyy 'at' h:mm tt} UTC"
            : "Duration: Indefinite (requires appeal for review)";

        return $@"ACCOUNT SUSPENSION NOTICE

Hello {_username},

We are writing to inform you that your Yapplr account has been suspended {suspensionDuration}.

SUSPENSION DETAILS:
Reason: {_reason}
Moderator: @{_moderatorUsername}
{durationMessage}

WHAT THIS MEANS:
• You cannot post, comment, or interact with content
• Your existing content remains visible to other users
• You can still view content but cannot participate
• You will not receive notifications during suspension

THINK THIS WAS A MISTAKE?
If you believe this suspension was issued in error, you can submit an appeal for review by our moderation team.

Submit an Appeal: {_appealUrl}

IMPORTANT: Please review our Community Guidelines at https://yapplr.com/community-guidelines before your suspension ends to ensure future compliance.

Best regards,
The Yapplr Moderation Team

© 2025 Yapplr. All rights reserved.";
    }
}
