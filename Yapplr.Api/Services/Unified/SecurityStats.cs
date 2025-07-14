namespace Yapplr.Api.Services.Unified;

/// <summary>
/// Security statistics
/// </summary>
public class SecurityStats
{
    public long TotalSecurityEvents { get; set; }
    public long BlockedNotifications { get; set; }
    public long RateLimitViolations { get; set; }
    public long ContentFilterViolations { get; set; }
    public Dictionary<string, long> ThreatBreakdown { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public long TotalEvents24h { get; set; }
    public Dictionary<string, int> EventTypeBreakdown { get; set; } = new();
    public Dictionary<string, int> SeverityBreakdown { get; set; } = new();
    public int CurrentlyBlockedUsers { get; set; }
    public long TotalViolations { get; set; }
}