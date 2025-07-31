namespace Yapplr.Api.Models;

/// <summary>
/// Unified model for notification content across all delivery channels.
/// Ensures consistent titles, bodies, and data across Firebase, SignalR, and Expo services.
/// </summary>
public class NotificationContent
{
    /// <summary>
    /// The notification title displayed to the user
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The notification body/message displayed to the user
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Additional data payload for the notification
    /// </summary>
    public Dictionary<string, string> Data { get; set; } = new();

    /// <summary>
    /// The type of notification (like, comment, follow, etc.)
    /// </summary>
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>
    /// Creates a new NotificationContent instance
    /// </summary>
    public NotificationContent()
    {
    }

    /// <summary>
    /// Creates a new NotificationContent instance with specified values
    /// </summary>
    public NotificationContent(string title, string body, string notificationType, Dictionary<string, string>? data = null)
    {
        Title = title;
        Body = body;
        NotificationType = notificationType;
        Data = data ?? new Dictionary<string, string>();
        
        // Always ensure the type is in the data dictionary
        Data["type"] = notificationType;
    }

    /// <summary>
    /// Adds or updates a data field
    /// </summary>
    public NotificationContent WithData(string key, string value)
    {
        Data[key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple data fields
    /// </summary>
    public NotificationContent WithData(Dictionary<string, string> additionalData)
    {
        foreach (var kvp in additionalData)
        {
            Data[kvp.Key] = kvp.Value;
        }
        return this;
    }
}
