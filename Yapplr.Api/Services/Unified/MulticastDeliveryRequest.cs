namespace Yapplr.Api.Services.Unified;

/// <summary>
/// Request for sending notifications to multiple users
/// </summary>
public class MulticastDeliveryRequest
{
    public List<int> UserIds { get; set; } = new();
    public string NotificationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string>? Data { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public bool RequireDeliveryConfirmation { get; set; } = false;
    public string? PreferredProvider { get; set; }
}