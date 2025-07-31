namespace Yapplr.Api.Services.Notifications;

/// <summary>
/// Request for delivering a notification through the provider system
/// </summary>
public class NotificationDeliveryRequest
{
    public int UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string>? Data { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public bool RequireDeliveryConfirmation { get; set; } = false;
    public string? PreferredProvider { get; set; }
    public List<string>? ExcludedProviders { get; set; }
}