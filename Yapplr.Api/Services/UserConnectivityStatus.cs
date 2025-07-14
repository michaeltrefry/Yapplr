namespace Yapplr.Api.Services;

/// <summary>
/// User connectivity status
/// </summary>
public class UserConnectivityStatus
{
    public int UserId { get; set; }
    public bool IsOnline { get; set; }
    public DateTime LastSeenAt { get; set; }
    public string? LastKnownConnection { get; set; } // "firebase", "signalr", "polling"
    public int OfflineNotificationCount { get; set; }
}