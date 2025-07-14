namespace Yapplr.Api.Configuration;

/// <summary>
/// SignalR notification provider configuration
/// </summary>
public class SignalRConfiguration
{
    public bool Enabled { get; set; } = true;
    public int MaxConnectionsPerUser { get; set; } = 10;
    public int MaxTotalConnections { get; set; } = 10000;
    public int CleanupIntervalMinutes { get; set; } = 30;
    public int InactivityThresholdHours { get; set; } = 2;
    public bool EnableDetailedErrors { get; set; } = false;
}