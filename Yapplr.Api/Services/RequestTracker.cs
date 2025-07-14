namespace Yapplr.Api.Services;

/// <summary>
/// Request tracking for rate limiting
/// </summary>
public class RequestTracker
{
    public Queue<DateTime> Requests { get; set; } = new();
    public DateTime LastRequest { get; set; }
    public int TotalRequests { get; set; }
}