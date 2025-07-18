namespace Yapplr.Api.Services.EmailTemplates;

public class NotificationDigestEmailTemplate : IEmailTemplate
{
    private readonly string _username;
    private readonly List<DigestNotification> _notifications;
    private readonly DateTime _periodStart;
    private readonly DateTime _periodEnd;
    private readonly string _unsubscribeUrl;

    public NotificationDigestEmailTemplate(
        string username,
        List<DigestNotification> notifications,
        DateTime periodStart,
        DateTime periodEnd,
        string? unsubscribeUrl = null)
    {
        _username = username;
        _notifications = notifications ?? new List<DigestNotification>();
        _periodStart = periodStart;
        _periodEnd = periodEnd;
        _unsubscribeUrl = unsubscribeUrl ?? "https://yapplr.com/settings/notifications";
    }

    public string Subject => GenerateSubject();

    private string GenerateSubject()
    {
        var count = _notifications.Count;
        var timeframe = GetTimeframeDescription();
        
        return count switch
        {
            0 => $"Your Yapplr digest for {timeframe}",
            1 => $"1 new notification on Yapplr ({timeframe})",
            _ => $"{count} new notifications on Yapplr ({timeframe})"
        };
    }

    private string GetTimeframeDescription()
    {
        var duration = _periodEnd - _periodStart;
        
        if (duration.TotalDays >= 6.5) // Weekly
            return "this week";
        else if (duration.TotalDays >= 0.9) // Daily
            return "today";
        else if (duration.TotalHours >= 0.9) // Hourly
            return "this hour";
        else
            return "recently";
    }

    public string GenerateHtmlBody()
    {
        var notificationCards = GenerateNotificationCards();
        var summaryStats = GenerateSummaryStats();
        var timeframe = GetTimeframeDescription();

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Your Yapplr Digest - {timeframe}</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; }}
        .header {{ background: linear-gradient(135deg, #1d9bf0 0%, #1a8cd8 100%); color: white; padding: 30px 20px; text-align: center; }}
        .logo {{ font-size: 24px; font-weight: bold; margin: 0; }}
        .digest-title {{ font-size: 16px; margin: 10px 0 0 0; opacity: 0.9; }}
        .content {{ padding: 30px; }}
        .summary-stats {{ background: #f8fafc; border-radius: 8px; padding: 20px; margin-bottom: 30px; }}
        .stat-grid {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(120px, 1fr)); gap: 15px; text-align: center; }}
        .stat-item {{ }}
        .stat-number {{ font-size: 24px; font-weight: bold; color: #1d9bf0; }}
        .stat-label {{ font-size: 12px; color: #64748b; text-transform: uppercase; }}
        .notification-card {{ background: #ffffff; border: 1px solid #e2e8f0; border-radius: 8px; padding: 15px; margin-bottom: 15px; }}
        .notification-header {{ display: flex; align-items: center; margin-bottom: 8px; }}
        .notification-icon {{ font-size: 18px; margin-right: 10px; }}
        .notification-type {{ font-size: 12px; color: #64748b; text-transform: uppercase; font-weight: 500; }}
        .notification-time {{ font-size: 12px; color: #94a3b8; margin-left: auto; }}
        .notification-title {{ font-weight: 600; color: #1e293b; margin-bottom: 5px; }}
        .notification-body {{ color: #475569; font-size: 14px; }}
        .action-button {{ display: inline-block; background: #1d9bf0; color: white; padding: 8px 16px; text-decoration: none; border-radius: 4px; font-size: 12px; margin-top: 10px; }}
        .footer {{ background: #f8f9fa; padding: 20px; text-align: center; color: #666; font-size: 14px; }}
        .footer a {{ color: #1d9bf0; text-decoration: none; }}
        .no-notifications {{ text-align: center; padding: 40px; color: #64748b; }}
        .divider {{ height: 1px; background: #e2e8f0; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 class='logo'>Yapplr</h1>
            <p class='digest-title'>Your notification digest for {timeframe}</p>
        </div>
        
        <div class='content'>
            <p>Hi {_username},</p>
            
            {summaryStats}
            
            {notificationCards}
            
            <div class='divider'></div>
            
            <p style='color: #64748b; font-size: 14px; text-align: center;'>
                This digest was sent because you have email digests enabled in your notification settings.
            </p>
        </div>

        <div class='footer'>
            <p><strong>Stay connected with Yapplr</strong></p>
            <p>
                <a href='https://yapplr.com'>Visit Yapplr</a> | 
                <a href='https://yapplr.com/notifications'>View All Notifications</a> | 
                <a href='https://yapplr.com/settings/notifications'>Settings</a>
            </p>
            <p style='margin-top: 15px; font-size: 12px;'>
                <a href='{_unsubscribeUrl}'>Manage digest preferences</a> | 
                <a href='https://yapplr.com/unsubscribe'>Unsubscribe from all emails</a>
            </p>
            <p style='margin-top: 10px;'>© 2025 Yapplr. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    public string GenerateTextBody()
    {
        var timeframe = GetTimeframeDescription();
        var notificationText = GenerateNotificationTextList();

        return $@"Yapplr Notification Digest

Hi {_username},

Here's your notification digest for {timeframe}:

{notificationText}

---

Stay connected with Yapplr
Visit: https://yapplr.com
View All Notifications: https://yapplr.com/notifications
Settings: https://yapplr.com/settings/notifications

Manage digest preferences: {_unsubscribeUrl}
Unsubscribe from all emails: https://yapplr.com/unsubscribe

© 2025 Yapplr. All rights reserved.";
    }

    private string GenerateSummaryStats()
    {
        if (_notifications.Count == 0)
            return "";

        var typeGroups = _notifications.GroupBy(n => n.Type).ToList();
        var statsHtml = new List<string>();

        // Total notifications
        statsHtml.Add($@"
            <div class='stat-item'>
                <div class='stat-number'>{_notifications.Count}</div>
                <div class='stat-label'>Total</div>
            </div>");

        // Top notification types
        foreach (var group in typeGroups.Take(3))
        {
            var icon = GetNotificationIcon(group.Key);
            statsHtml.Add($@"
                <div class='stat-item'>
                    <div class='stat-number'>{icon} {group.Count()}</div>
                    <div class='stat-label'>{FormatNotificationType(group.Key)}</div>
                </div>");
        }

        return $@"
            <div class='summary-stats'>
                <div class='stat-grid'>
                    {string.Join("", statsHtml)}
                </div>
            </div>";
    }

    private string GenerateNotificationCards()
    {
        if (_notifications.Count == 0)
        {
            return @"
                <div class='no-notifications'>
                    <p>🎉 You're all caught up!</p>
                    <p>No new notifications for this period.</p>
                </div>";
        }

        var cards = _notifications.Take(10).Select(notification => // Limit to 10 most recent
        {
            var icon = GetNotificationIcon(notification.Type);
            var timeAgo = GetTimeAgo(notification.CreatedAt);
            var actionButton = !string.IsNullOrEmpty(notification.ActionUrl)
                ? $"<a href='{notification.ActionUrl}' class='action-button'>View</a>"
                : "";

            return $@"
                <div class='notification-card'>
                    <div class='notification-header'>
                        <span class='notification-icon'>{icon}</span>
                        <span class='notification-type'>{FormatNotificationType(notification.Type)}</span>
                        <span class='notification-time'>{timeAgo}</span>
                    </div>
                    <div class='notification-title'>{notification.Title}</div>
                    <div class='notification-body'>{notification.Body}</div>
                    {actionButton}
                </div>";
        });

        var moreCount = _notifications.Count - 10;
        var moreText = moreCount > 0 
            ? $"<p style='text-align: center; color: #64748b; margin-top: 20px;'>+ {moreCount} more notifications</p>"
            : "";

        return string.Join("", cards) + moreText;
    }

    private string GenerateNotificationTextList()
    {
        if (_notifications.Count == 0)
            return "🎉 You're all caught up! No new notifications for this period.";

        var textList = _notifications.Take(10).Select(notification =>
        {
            var icon = GetNotificationIcon(notification.Type);
            var timeAgo = GetTimeAgo(notification.CreatedAt);
            return $"{icon} {notification.Title}\n   {notification.Body}\n   {timeAgo}";
        });

        var moreCount = _notifications.Count - 10;
        var moreText = moreCount > 0 ? $"\n+ {moreCount} more notifications" : "";

        return string.Join("\n\n", textList) + moreText;
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

    private static string FormatNotificationType(string type)
    {
        return type.ToLower() switch
        {
            "follow_request" => "Follow Requests",
            "video_processing" => "Video Processing",
            _ => char.ToUpper(type[0]) + type[1..].ToLower() + "s"
        };
    }

    private static string GetTimeAgo(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;
        
        return timeSpan.TotalDays switch
        {
            >= 1 => $"{(int)timeSpan.TotalDays}d ago",
            _ when timeSpan.TotalHours >= 1 => $"{(int)timeSpan.TotalHours}h ago",
            _ => $"{(int)timeSpan.TotalMinutes}m ago"
        };
    }
}

public class DigestNotification
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? ActionUrl { get; set; }
}
