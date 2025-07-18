namespace Yapplr.Api.Services.EmailTemplates;

public class NotificationEmailTemplate : IEmailTemplate
{
    private readonly string _username;
    private readonly string _notificationTitle;
    private readonly string _notificationBody;
    private readonly string _notificationType;
    private readonly string? _actionUrl;
    private readonly Dictionary<string, string>? _data;

    public NotificationEmailTemplate(
        string username, 
        string notificationTitle, 
        string notificationBody, 
        string notificationType,
        string? actionUrl = null,
        Dictionary<string, string>? data = null)
    {
        _username = username;
        _notificationTitle = notificationTitle;
        _notificationBody = notificationBody;
        _notificationType = notificationType;
        _actionUrl = actionUrl;
        _data = data;
    }

    public string Subject => _notificationTitle;

    public string GenerateHtmlBody()
    {
        var notificationIcon = GetNotificationIcon(_notificationType);
        var actionButton = !string.IsNullOrEmpty(_actionUrl) 
            ? $@"<div style='text-align: center; margin: 30px 0;'>
                    <a href='{_actionUrl}' class='action-button'>View on Yapplr</a>
                 </div>"
            : "";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{_notificationTitle} - Yapplr</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; }}
        .header {{ background: linear-gradient(135deg, #1d9bf0 0%, #1a8cd8 100%); color: white; padding: 30px 20px; text-align: center; }}
        .logo {{ font-size: 24px; font-weight: bold; margin: 0; }}
        .content {{ padding: 30px; }}
        .notification-card {{ background: #f8fafc; border-left: 4px solid #1d9bf0; padding: 20px; margin: 20px 0; border-radius: 0 8px 8px 0; }}
        .notification-icon {{ font-size: 24px; margin-bottom: 10px; }}
        .notification-title {{ font-size: 18px; font-weight: bold; color: #1e293b; margin: 0 0 10px 0; }}
        .notification-body {{ color: #475569; margin: 0; }}
        .action-button {{ display: inline-block; background: linear-gradient(135deg, #1d9bf0 0%, #1a8cd8 100%); color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; }}
        .action-button:hover {{ background: linear-gradient(135deg, #1a8cd8 0%, #1570b8 100%); }}
        .footer {{ background: #f8f9fa; padding: 20px; text-align: center; color: #666; font-size: 14px; }}
        .footer a {{ color: #1d9bf0; text-decoration: none; }}
        .unsubscribe {{ margin-top: 15px; font-size: 12px; }}
        .divider {{ height: 1px; background: #e2e8f0; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 class='logo'>Yapplr</h1>
            <p style='margin: 5px 0 0 0; opacity: 0.9; font-size: 14px;'>You have a new notification</p>
        </div>
        
        <div class='content'>
            <p>Hi {_username},</p>
            
            <div class='notification-card'>
                <div class='notification-icon'>{notificationIcon}</div>
                <h2 class='notification-title'>{_notificationTitle}</h2>
                <p class='notification-body'>{_notificationBody}</p>
            </div>

            {actionButton}

            <div class='divider'></div>
            
            <p style='color: #64748b; font-size: 14px;'>
                This notification was sent because you have email notifications enabled in your Yapplr settings. 
                You can manage your notification preferences anytime in your account settings.
            </p>
        </div>

        <div class='footer'>
            <p><strong>Stay connected with Yapplr</strong></p>
            <p>
                <a href='https://yapplr.com'>Visit Yapplr</a> | 
                <a href='https://yapplr.com/settings/notifications'>Notification Settings</a> | 
                <a href='https://yapplr.com/support'>Support</a>
            </p>
            <div class='unsubscribe'>
                <a href='https://yapplr.com/settings/notifications'>Manage email preferences</a> | 
                <a href='https://yapplr.com/unsubscribe'>Unsubscribe from all emails</a>
            </div>
            <p style='margin-top: 15px;'>© 2025 Yapplr. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    public string GenerateTextBody()
    {
        var actionText = !string.IsNullOrEmpty(_actionUrl) 
            ? $"\n\nView on Yapplr: {_actionUrl}\n"
            : "";

        return $@"Yapplr Notification

Hi {_username},

{_notificationTitle}

{_notificationBody}
{actionText}
This notification was sent because you have email notifications enabled in your Yapplr settings. You can manage your notification preferences anytime in your account settings.

---

Stay connected with Yapplr
Visit: https://yapplr.com
Notification Settings: https://yapplr.com/settings/notifications
Support: https://yapplr.com/support

Manage email preferences: https://yapplr.com/settings/notifications
Unsubscribe from all emails: https://yapplr.com/unsubscribe

© 2025 Yapplr. All rights reserved.";
    }

    private static string GetNotificationIcon(string notificationType)
    {
        return notificationType.ToLower() switch
        {
            "message" => "💬",
            "mention" => "🏷️",
            "reply" => "↩️",
            "comment" => "💭",
            "follow" => "👥",
            "like" => "❤️",
            "repost" => "🔄",
            "follow_request" => "👋",
            "system" => "🔔",
            "moderation" => "⚠️",
            "video_processing" => "🎬",
            _ => "🔔"
        };
    }
}
