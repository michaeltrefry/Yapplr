namespace Yapplr.Api.Configuration;

/// <summary>
/// Configuration for notification providers
/// </summary>
public class NotificationProvidersConfiguration
{
    public const string SectionName = "NotificationProviders";
    
    public FirebaseConfiguration Firebase { get; set; } = new();
    public SignalRConfiguration SignalR { get; set; } = new();
}

/// <summary>
/// Firebase notification provider configuration
/// </summary>
public class FirebaseConfiguration
{
    public bool Enabled { get; set; } = true;
    public string ProjectId { get; set; } = string.Empty;
    public string ServiceAccountKeyFile { get; set; } = string.Empty;
}

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
