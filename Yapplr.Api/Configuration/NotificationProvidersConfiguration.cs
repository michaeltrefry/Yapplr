namespace Yapplr.Api.Configuration;

/// <summary>
/// Configuration for notification providers
/// </summary>
public class NotificationProvidersConfiguration
{
    public const string SectionName = "NotificationProviders";

    public FirebaseConfiguration Firebase { get; set; } = new();
    public SignalRConfiguration SignalR { get; set; } = new();
    public ExpoConfiguration Expo { get; set; } = new();
}