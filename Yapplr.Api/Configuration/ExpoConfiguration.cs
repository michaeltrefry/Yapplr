namespace Yapplr.Api.Configuration;

/// <summary>
/// Expo notification provider configuration
/// </summary>
public class ExpoConfiguration
{
    public bool Enabled { get; set; } = true;
    public string? AccessToken { get; set; }
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 30;
}
